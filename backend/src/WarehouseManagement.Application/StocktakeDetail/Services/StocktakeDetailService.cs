// Lớp Application — dịch vụ dòng chi tiết kiểm kê (báo cáo + CRUD trên phiếu Draft).
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Services;

public class StocktakeDetailService : IStocktakeDetailService
{
    private readonly IStocktakeDetailRepository _stocktakeDetailRepository;
    private readonly IStocktakeRepository _stocktakeRepo;
    private readonly IProductRepository _productRepository;
    private readonly IWarehouseStockRepository _warehouseStockRepository;

    public StocktakeDetailService(
        IStocktakeDetailRepository stocktakeDetailRepository,
        IStocktakeRepository stocktakeRepo,
        IProductRepository productRepository,
        IWarehouseStockRepository warehouseStockRepository)
    {
        _stocktakeDetailRepository = stocktakeDetailRepository;
        _stocktakeRepo = stocktakeRepo;
        _productRepository = productRepository;
        _warehouseStockRepository = warehouseStockRepository;
    }

    // Báo cáo / danh sách chi tiết + chênh lệch (có lọc onlyDiff).
    public async Task<PagedResult<StocktakeDetailItemDto>> GetAllAsync(
        PaginationQuery query,
        int? stocktakeId = null,
        int? warehouseId = null,
        int? productId = null,
        StocktakeStatus? status = null,
        bool? onlyDiff = null)
    {
        var pagedResult = await _stocktakeDetailRepository.GetPagedAsync(
            query.Search?.Trim(),
            stocktakeId,
            warehouseId,
            productId,
            status,
            onlyDiff,
            query.PageNumber,
            query.PageSize);

        return new PagedResult<StocktakeDetailItemDto>
        {
            Items = pagedResult.Items.Select(ToResponseDto).ToList(),
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize,
            TotalRecords = pagedResult.TotalRecords
        };
    }

    // Lấy một dòng chi tiết theo Id.
    public async Task<StocktakeDetailItemDto> GetByIdAsync(int id)
    {
        if (id <= 0)
            throw new ValidationException("Id không hợp lệ.");

        var detail = await _stocktakeDetailRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Không tìm thấy chi tiết kiểm kê");

        return ToResponseDto(detail);
    }

    // Thêm dòng SP vào phiếu Draft; snapshot tồn hệ thống và tính Difference.
    public async Task<StocktakeDetailItemDto> AddAsync(int stocktakeId, StocktakeDetailRequestDto dto)
    {
        var stocktake = await GetDraftStocktakeAsync(stocktakeId);

        var product = await _productRepository.GetByIdAsync(dto.ProductId)
            ?? throw new ValidationException($"ProductId {dto.ProductId} không tồn tại.");

        if (!product.IsActive)
            throw new ValidationException($"Sản phẩm {product.Sku} đang ngừng hoạt động.");

        if (await _stocktakeDetailRepository.ExistsProductAsync(stocktakeId, dto.ProductId))
            throw new ConflictException("Sản phẩm đã có trên phiếu kiểm kê này");

        // Snapshot tồn hiện tại làm SystemQuantity
        var systemQty = await GetSystemQtyAsync(stocktake.WarehouseId, dto.ProductId);

        var detail = new StocktakeDetail
        {
            StocktakeId = stocktakeId,
            ProductId = dto.ProductId,
            SystemQuantity = systemQty,
            CountedQuantity = dto.CountedQuantity,
            Difference = dto.CountedQuantity - systemQty,
            Note = dto.Note?.Trim()
        };

        await _stocktakeDetailRepository.AddAsync(detail);

        stocktake.UpdatedAt = DateTime.UtcNow;
        _stocktakeRepo.Update(stocktake);
        await _stocktakeDetailRepository.SaveChangesAsync();

        return ToResponseDto(await _stocktakeDetailRepository.GetByIdAsync(detail.Id)
            ?? throw new NotFoundException("Không tìm thấy chi tiết kiểm kê"));
    }

    // Cập nhật số đếm trên Draft; snapshot lại tồn hệ thống rồi tính lại lệch.
    public async Task<StocktakeDetailItemDto> UpdateAsync(
        int stocktakeId,
        int detailId,
        StocktakeDetailUpdateDto dto)
    {
        var stocktake = await GetDraftStocktakeAsync(stocktakeId);

        var detail = await _stocktakeDetailRepository.GetByStocktakeAndIdAsync(stocktakeId, detailId)
            ?? throw new NotFoundException("Không tìm thấy chi tiết kiểm kê");

        // Snapshot lại tồn hệ thống khi sửa dòng (Draft)
        var systemQty = await GetSystemQtyAsync(stocktake.WarehouseId, detail.ProductId);

        detail.SystemQuantity = systemQty;
        detail.CountedQuantity = dto.CountedQuantity;
        detail.Difference = dto.CountedQuantity - systemQty;
        detail.Note = dto.Note?.Trim();
        detail.UpdatedAt = DateTime.UtcNow;

        _stocktakeDetailRepository.Update(detail);

        stocktake.UpdatedAt = DateTime.UtcNow;
        _stocktakeRepo.Update(stocktake);
        await _stocktakeDetailRepository.SaveChangesAsync();

        return ToResponseDto(await _stocktakeDetailRepository.GetByIdAsync(detail.Id)
            ?? throw new NotFoundException("Không tìm thấy chi tiết kiểm kê"));
    }

    // Xóa dòng chi tiết khỏi phiếu Draft.
    public async Task DeleteAsync(int stocktakeId, int detailId)
    {
        await GetDraftStocktakeAsync(stocktakeId);

        var detail = await _stocktakeDetailRepository.GetByStocktakeAndIdAsync(stocktakeId, detailId)
            ?? throw new NotFoundException("Không tìm thấy chi tiết kiểm kê");

        _stocktakeDetailRepository.Delete(detail);
        await _stocktakeDetailRepository.SaveChangesAsync();
    }

    // Lấy tồn hệ thống theo kho–SP (0 nếu chưa có dòng tồn).
    private async Task<decimal> GetSystemQtyAsync(int warehouseId, int productId)
    {
        var stock = await _warehouseStockRepository.GetByWarehouseAndProductAsync(warehouseId, productId);
        return stock?.Quantity ?? 0m;
    }

    // Chỉ cho phép thao tác khi phiếu còn Draft.
    private async Task<Stocktake> GetDraftStocktakeAsync(int stocktakeId)
    {
        if (stocktakeId <= 0)
            throw new ValidationException("StocktakeId không hợp lệ.");

        var stocktake = await _stocktakeRepo.GetByIdAsync(stocktakeId)
            ?? throw new NotFoundException("Không tìm thấy phiếu kiểm kê");

        if (stocktake.Status != StocktakeStatus.Draft)
            throw new ConflictException("Chỉ được sửa chi tiết khi phiếu còn ở trạng thái Draft.");

        return stocktake;
    }

    // Map entity → DTO báo cáo (kèm HasDifference).
    private static StocktakeDetailItemDto ToResponseDto(StocktakeDetail detail) => new()
    {
        Id = detail.Id,
        StocktakeId = detail.StocktakeId,
        StocktakeCode = detail.Stocktake?.Code,
        StocktakeStatus = detail.Stocktake?.Status.ToString(),
        StocktakeDate = detail.Stocktake?.StocktakeDate ?? default,
        WarehouseId = detail.Stocktake?.WarehouseId ?? 0,
        WarehouseCode = detail.Stocktake?.Warehouse?.Code,
        WarehouseName = detail.Stocktake?.Warehouse?.Name,
        ProductId = detail.ProductId,
        ProductSku = detail.Product?.Sku,
        ProductName = detail.Product?.Name,
        UnitSymbol = detail.Product?.Unit?.Symbol,
        SystemQuantity = detail.SystemQuantity,
        CountedQuantity = detail.CountedQuantity,
        Difference = detail.Difference,
        HasDifference = detail.Difference != 0,
        Note = detail.Note,
        CreatedAt = detail.CreatedAt,
        UpdatedAt = detail.UpdatedAt
    };
}
