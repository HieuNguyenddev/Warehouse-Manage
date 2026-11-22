// Lớp Application — dịch vụ nghiệp vụ tồn kho theo kho–sản phẩm (mở tồn, điều chỉnh, min).
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Services;

public class WarehouseStockService : IWarehouseStockService
{
    private readonly IWarehouseStockRepository _warehouseStockRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStockMovementService _stockMovementService;

    public WarehouseStockService(
        IWarehouseStockRepository warehouseStockRepository,
        IWarehouseRepository warehouseRepository,
        IProductRepository productRepository,
        IStockMovementService stockMovementService)
    {
        _warehouseStockRepository = warehouseStockRepository;
        _warehouseRepository = warehouseRepository;
        _productRepository = productRepository;
        _stockMovementService = stockMovementService;
    }

    // Lấy dòng tồn theo Id.
    public async Task<WarehouseStockResponseDto> GetByIdAsync(int id)
        => ToResponseDto(await GetStockAsync(id));

    // Lấy danh sách tồn phân trang; lọc kho / SP / tồn thấp / hết hàng.
    public async Task<PagedResult<WarehouseStockResponseDto>> GetAllAsync(
        PaginationQuery query,
        int? warehouseId = null,
        int? productId = null,
        bool? lowStockOnly = null,
        bool? outOfStockOnly = null)
    {
        var pagedResult = await _warehouseStockRepository.GetPagedAsync(
            query.Search?.Trim(),
            warehouseId,
            productId,
            lowStockOnly,
            outOfStockOnly,
            query.PageNumber,
            query.PageSize);

        return new PagedResult<WarehouseStockResponseDto>
        {
            Items = pagedResult.Items.Select(ToResponseDto).ToList(),
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize,
            TotalRecords = pagedResult.TotalRecords
        };
    }

    // Tổng quan tồn kho (số SKU, low/out, tổng qty).
    public async Task<WarehouseStockSummaryDto> GetSummaryAsync(int? warehouseId = null)
    {
        var (total, low, outOfStock, qty) = await _warehouseStockRepository.GetSummaryAsync(warehouseId);
        return new WarehouseStockSummaryDto
        {
            WarehouseId = warehouseId,
            TotalSkus = total,
            LowStockCount = low,
            OutOfStockCount = outOfStock,
            TotalQuantity = qty
        };
    }

    // API cũ — ủy quyền sang CreateOpeningAsync.
    public Task<WarehouseStockResponseDto> CreateAsync(WarehouseStockRequestDto dto)
        => CreateOpeningAsync(dto);

    // Khởi tạo tồn đầu kỳ: chỉ khi chưa có dòng tồn; ghi sổ Manual.
    public async Task<WarehouseStockResponseDto> CreateOpeningAsync(WarehouseStockRequestDto dto)
    {
        await EnsureRefsAsync(dto.WarehouseId, dto.ProductId);

        // Không cho mở tồn lần 2 — phải dùng Adjust
        var existing = await _warehouseStockRepository.GetByWarehouseAndProductAsync(dto.WarehouseId, dto.ProductId);
        if (existing is not null)
            throw new ConflictException("Sản phẩm đã có tồn kho tại kho này. Hãy dùng điều chỉnh tồn.");

        var reason = NormalizeReason(dto.Reason, "Khởi tạo tồn đầu kỳ");

        // Gán tồn tuyệt đối qua StockMovement (tạo dòng + ghi sổ)
        await _stockMovementService.SetQuantityAsync(
            dto.WarehouseId,
            dto.ProductId,
            dto.Quantity,
            StockTransactionType.Manual,
            StockReferenceTypes.WarehouseStock,
            null,
            null,
            reason);

        await _warehouseStockRepository.SaveChangesAsync();

        var stock = await _warehouseStockRepository.GetByWarehouseAndProductAsync(dto.WarehouseId, dto.ProductId)
            ?? throw new NotFoundException("Không tìm thấy tồn kho");

        // MinQuantity tùy chọn gắn sau khi đã có dòng tồn
        if (dto.MinQuantity is not null)
        {
            stock.MinQuantity = dto.MinQuantity;
            stock.UpdatedAt = DateTime.UtcNow;
            _warehouseStockRepository.Update(stock);
            await _warehouseStockRepository.SaveChangesAsync();
        }

        return ToResponseDto(await GetStockAsync(stock.Id));
    }

    // Điều chỉnh tồn tuyệt đối kèm lý do (ghi sổ Manual).
    public async Task<WarehouseStockResponseDto> AdjustAsync(int id, WarehouseStockAdjustDto dto)
    {
        var stock = await GetStockAsync(id);
        var reason = NormalizeReason(dto.Reason, "Điều chỉnh tồn");

        await _stockMovementService.SetQuantityAsync(
            stock.WarehouseId,
            stock.ProductId,
            dto.Quantity,
            StockTransactionType.Manual,
            StockReferenceTypes.WarehouseStock,
            stock.Id,
            null,
            reason);

        await _warehouseStockRepository.SaveChangesAsync();
        return ToResponseDto(await GetStockAsync(stock.Id));
    }

    // Cập nhật ngưỡng min theo kho (không đụng số lượng tồn).
    public async Task<WarehouseStockResponseDto> UpdateMinAsync(int id, WarehouseStockMinDto dto)
    {
        var stock = await GetStockAsync(id);
        stock.MinQuantity = dto.MinQuantity;
        stock.UpdatedAt = DateTime.UtcNow;
        _warehouseStockRepository.Update(stock);
        await _warehouseStockRepository.SaveChangesAsync();
        return ToResponseDto(await GetStockAsync(stock.Id));
    }

    // API cũ — map sang AdjustAsync với lý do mặc định nếu thiếu.
    public Task<WarehouseStockResponseDto> UpdateAsync(int id, WarehouseStockUpdateDto dto)
        => AdjustAsync(id, new WarehouseStockAdjustDto
        {
            Quantity = dto.Quantity,
            Reason = string.IsNullOrWhiteSpace(dto.Reason) ? "Điều chỉnh tồn thủ công" : dto.Reason
        });

    // Kiểm tra kho/SP tồn tại và đang hoạt động.
    private async Task EnsureRefsAsync(int warehouseId, int productId)
    {
        var warehouse = await _warehouseRepository.GetByIdAsync(warehouseId)
            ?? throw new ValidationException("WarehouseId không tồn tại.");
        if (!warehouse.IsActive)
            throw new ValidationException("Kho hàng đang ngừng hoạt động.");

        var product = await _productRepository.GetByIdAsync(productId)
            ?? throw new ValidationException("ProductId không tồn tại.");
        if (!product.IsActive)
            throw new ValidationException("Sản phẩm đang ngừng hoạt động.");
    }

    // Lấy entity tồn hoặc ném lỗi.
    private async Task<WarehouseStock> GetStockAsync(int id)
    {
        if (id <= 0)
            throw new ValidationException("Id không hợp lệ.");

        var stock = await _warehouseStockRepository.GetByIdAsync(id);
        if (stock is null)
            throw new NotFoundException("Không tìm thấy tồn kho");

        return stock;
    }

    // Chuẩn hóa lý do ghi sổ (fallback + cắt 255 ký tự).
    private static string NormalizeReason(string? reason, string fallback)
    {
        var text = reason?.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return fallback;
        return text.Length > 255 ? text[..255] : text;
    }

    // Ngưỡng hiệu lực: ưu tiên MinQuantity theo kho, không thì Product.MinStock.
    internal static int ResolveEffectiveMin(WarehouseStock stock)
    {
        if (stock.MinQuantity is not null)
            return stock.MinQuantity.Value;
        return stock.Product?.MinStock ?? 0;
    }

    // Cảnh báo tồn thấp khi có ngưỡng > 0 và qty <= ngưỡng.
    internal static bool ResolveIsLowStock(decimal quantity, int effectiveMin)
        => effectiveMin > 0 && quantity <= effectiveMin;

    // Map entity → DTO (tính EffectiveMin / IsLowStock / IsOutOfStock).
    private static WarehouseStockResponseDto ToResponseDto(WarehouseStock stock)
    {
        var productMin = stock.Product?.MinStock ?? 0;
        var effectiveMin = ResolveEffectiveMin(stock);

        return new WarehouseStockResponseDto
        {
            Id = stock.Id,
            WarehouseId = stock.WarehouseId,
            WarehouseCode = stock.Warehouse?.Code,
            WarehouseName = stock.Warehouse?.Name,
            ProductId = stock.ProductId,
            ProductSku = stock.Product?.Sku,
            ProductName = stock.Product?.Name,
            CategoryName = stock.Product?.Category?.Name,
            UnitSymbol = stock.Product?.Unit?.Symbol,
            ProductMinStock = productMin,
            MinQuantity = stock.MinQuantity,
            EffectiveMinStock = effectiveMin,
            Quantity = stock.Quantity,
            IsLowStock = ResolveIsLowStock(stock.Quantity, effectiveMin),
            IsOutOfStock = stock.Quantity <= 0,
            CreatedAt = stock.CreatedAt,
            UpdatedAt = stock.UpdatedAt
        };
    }
}
