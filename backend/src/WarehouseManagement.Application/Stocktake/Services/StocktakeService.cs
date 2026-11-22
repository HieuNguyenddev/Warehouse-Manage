// Lớp Application — dịch vụ nghiệp vụ phiếu kiểm kê (manual/auto, confirm áp tồn, cancel đảo delta).
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Services;

public class StocktakeService : IStocktakeService
{
    private readonly IStocktakeRepository _repo;
    private readonly IWarehouseRepository _warehouseRepo;
    private readonly IProductRepository _productRepo;
    private readonly IWarehouseStockRepository _stockRepo;
    private readonly IStockMovementService _movement;

    public StocktakeService(
        IStocktakeRepository repo,
        IWarehouseRepository warehouseRepo,
        IProductRepository productRepo,
        IWarehouseStockRepository stockRepo,
        IStockMovementService movement)
    {
        _repo = repo;
        _warehouseRepo = warehouseRepo;
        _productRepo = productRepo;
        _stockRepo = stockRepo;
        _movement = movement;
    }

    public async Task<StocktakeResponseDto> GetByIdAsync(int id)
        => Map(await GetStocktakeAsync(id));

    public async Task<PagedResult<StocktakeResponseDto>> GetAllAsync(
        PaginationQuery query,
        int? warehouseId = null,
        StocktakeStatus? status = null)
    {
        var paged = await _repo.GetPagedAsync(
            query.Search?.Trim(),
            warehouseId,
            status,
            query.PageNumber,
            query.PageSize);

        return new PagedResult<StocktakeResponseDto>
        {
            Items = paged.Items.Select(Map).ToList(),
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalRecords = paged.TotalRecords
        };
    }

    // Tạo phiếu kiểm kê thủ công ở trạng thái Draft.
    public async Task<StocktakeResponseDto> CreateAsync(StocktakeRequestDto dto)
    {
        await EnsureWarehouseAsync(dto.WarehouseId);
        var details = await BuildDetailsAsync(dto.WarehouseId, dto.Details);

        var stocktake = new Stocktake
        {
            Code = await GenerateCodeAsync(),
            StocktakeDate = dto.StocktakeDate?.ToUniversalTime() ?? DateTime.UtcNow,
            WarehouseId = dto.WarehouseId,
            Note = dto.Note?.Trim(),
            Status = StocktakeStatus.Draft,
            Details = details
        };

        await _repo.AddAsync(stocktake);
        await _repo.SaveChangesAsync();

        return Map(await GetStocktakeAsync(stocktake.Id));
    }

    // Tạo phiếu kiểm kho tự động từ tồn hiện tại của kho
    public async Task<StocktakeResponseDto> CreateAutoAsync(AutoStocktakeRequestDto dto)
    {
        await EnsureWarehouseAsync(dto.WarehouseId);

        var stocks = await _stockRepo.GetListByWarehouseAsync(dto.WarehouseId, take: 200);
        if (dto.LowStockOnly)
            stocks = stocks.Where(x => x.Product != null && x.Quantity <= x.Product.MinStock).ToList();

        if (stocks.Count == 0)
            throw new ValidationException(
                dto.LowStockOnly
                    ? "Kho không có mặt hàng tồn thấp để kiểm tự động."
                    : "Kho chưa có tồn sản phẩm để kiểm tự động.");

        var lines = stocks.Select(s => new StocktakeDetailRequestDto
        {
            ProductId = s.ProductId,
            CountedQuantity = dto.MatchSystemQuantity ? s.Quantity : 0m,
            Note = "Tự động"
        }).ToList();

        var note = string.IsNullOrWhiteSpace(dto.Note)
            ? (dto.LowStockOnly ? "Kiểm kho tự động · chỉ tồn thấp" : "Kiểm kho tự động · toàn bộ tồn kho")
            : dto.Note.Trim();

        return await CreateAsync(new StocktakeRequestDto
        {
            WarehouseId = dto.WarehouseId,
            Note = note,
            Details = lines
        });
    }

    public async Task<StocktakeResponseDto> UpdateAsync(int id, StocktakeRequestDto dto)
    {
        var stocktake = await GetStocktakeAsync(id);
        EnsureDraft(stocktake);

        await EnsureWarehouseAsync(dto.WarehouseId);
        var details = await BuildDetailsAsync(dto.WarehouseId, dto.Details);

        if (stocktake.Details.Count > 0)
            _repo.RemoveDetails(stocktake.Details.ToList());

        stocktake.WarehouseId = dto.WarehouseId;
        stocktake.StocktakeDate = dto.StocktakeDate?.ToUniversalTime() ?? stocktake.StocktakeDate;
        stocktake.Note = dto.Note?.Trim();
        stocktake.UpdatedAt = DateTime.UtcNow;
        stocktake.Details = details;

        _repo.Update(stocktake);
        await _repo.SaveChangesAsync();

        return Map(await GetStocktakeAsync(stocktake.Id));
    }

    // Xác nhận → gán tồn = CountedQuantity
    public async Task<StocktakeResponseDto> ConfirmAsync(int id)
    {
        var stocktake = await GetStocktakeAsync(id);
        EnsureDraft(stocktake);

        if (stocktake.Details.Count == 0)
            throw new ValidationException("Phiếu kiểm kê không có chi tiết.");

        foreach (var line in stocktake.Details)
        {
            await _movement.SetQuantityAsync(
                stocktake.WarehouseId,
                line.ProductId,
                line.CountedQuantity,
                StockTransactionType.Stocktake,
                StockReferenceTypes.Stocktake,
                stocktake.Id,
                stocktake.Code,
                $"Kiểm kê {stocktake.Code}");
        }

        stocktake.Status = StocktakeStatus.Confirmed;
        stocktake.ConfirmedAt = DateTime.UtcNow;
        stocktake.UpdatedAt = DateTime.UtcNow;

        _repo.Update(stocktake);
        await _repo.SaveChangesAsync();

        return Map(await GetStocktakeAsync(stocktake.Id));
    }

    // Hủy:
    // - Draft: chỉ đổi trạng thái
    // - Confirmed: hoàn tồn về SystemQuantity (snapshot lúc lập phiếu)
    public async Task<StocktakeResponseDto> CancelAsync(int id)
    {
        var stocktake = await GetStocktakeAsync(id);

        if (stocktake.Status == StocktakeStatus.Cancelled)
            throw new ConflictException("Phiếu kiểm kê đã bị hủy");

        if (stocktake.Status == StocktakeStatus.Confirmed)
        {
            foreach (var line in stocktake.Details)
            {
                await _movement.SetQuantityAsync(
                    stocktake.WarehouseId,
                    line.ProductId,
                    line.SystemQuantity,
                    StockTransactionType.StocktakeCancel,
                    StockReferenceTypes.Stocktake,
                    stocktake.Id,
                    stocktake.Code,
                    $"Hủy kiểm kê {stocktake.Code}");
            }
        }

        stocktake.Status = StocktakeStatus.Cancelled;
        stocktake.CancelledAt = DateTime.UtcNow;
        stocktake.UpdatedAt = DateTime.UtcNow;

        _repo.Update(stocktake);
        await _repo.SaveChangesAsync();

        return Map(await GetStocktakeAsync(stocktake.Id));
    }

    public async Task DeleteAsync(int id)
    {
        var stocktake = await GetStocktakeAsync(id);
        EnsureDraft(stocktake);

        _repo.Delete(stocktake);
        await _repo.SaveChangesAsync();
    }

    private async Task EnsureWarehouseAsync(int warehouseId)
    {
        var warehouse = await _warehouseRepo.GetByIdAsync(warehouseId)
            ?? throw new ValidationException("WarehouseId không tồn tại.");

        if (!warehouse.IsActive)
            throw new ValidationException("Kho hàng đang ngừng hoạt động.");
    }

    private async Task<List<StocktakeDetail>> BuildDetailsAsync(
        int warehouseId,
        List<StocktakeDetailRequestDto> lines)
    {
        var result = new List<StocktakeDetail>();

        foreach (var line in lines)
        {
            var product = await _productRepo.GetByIdAsync(line.ProductId)
                ?? throw new ValidationException($"ProductId {line.ProductId} không tồn tại.");

            if (!product.IsActive)
                throw new ValidationException($"Sản phẩm {product.Sku} đang ngừng hoạt động.");

            var stock = await _stockRepo.GetByWarehouseAndProductAsync(warehouseId, line.ProductId);
            var systemQty = stock?.Quantity ?? 0m;

            result.Add(new StocktakeDetail
            {
                ProductId = line.ProductId,
                SystemQuantity = systemQty,
                CountedQuantity = line.CountedQuantity,
                Difference = line.CountedQuantity - systemQty,
                Note = line.Note?.Trim()
            });
        }

        return result;
    }

    private async Task<string> GenerateCodeAsync()
    {
        var prefix = $"KK-{DateTime.UtcNow:yyyyMMdd}-";
        var count = await _repo.CountByCodePrefixAsync(prefix);
        return $"{prefix}{(count + 1):D4}";
    }

    private static void EnsureDraft(Stocktake stocktake)
    {
        if (stocktake.Status != StocktakeStatus.Draft)
            throw new ConflictException("Chỉ được thao tác khi phiếu còn ở trạng thái Draft.");
    }

    private async Task<Stocktake> GetStocktakeAsync(int id)
    {
        if (id <= 0)
            throw new ValidationException("Id không hợp lệ.");

        var stocktake = await _repo.GetByIdAsync(id);
        if (stocktake is null)
            throw new NotFoundException("Không tìm thấy phiếu kiểm kê");

        return stocktake;
    }

    private static StocktakeResponseDto Map(Stocktake s)
    {
        var details = s.Details
            .OrderBy(x => x.Id)
            .Select(d => new StocktakeDetailResponseDto
            {
                Id = d.Id,
                ProductId = d.ProductId,
                ProductSku = d.Product?.Sku,
                ProductName = d.Product?.Name,
                UnitSymbol = d.Product?.Unit?.Symbol,
                SystemQuantity = d.SystemQuantity,
                CountedQuantity = d.CountedQuantity,
                Difference = d.Difference,
                Note = d.Note
            })
            .ToList();

        return new StocktakeResponseDto
        {
            Id = s.Id,
            Code = s.Code,
            StocktakeDate = s.StocktakeDate,
            WarehouseId = s.WarehouseId,
            WarehouseCode = s.Warehouse?.Code,
            WarehouseName = s.Warehouse?.Name,
            Status = s.Status.ToString(),
            Note = s.Note,
            TotalLines = details.Count,
            DiffLines = details.Count(x => x.Difference != 0),
            ConfirmedAt = s.ConfirmedAt,
            CancelledAt = s.CancelledAt,
            Details = details,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        };
    }
}
