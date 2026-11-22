using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Infrastructure.Persistence;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Infrastructure.Repositories;

/// <summary>
/// Truy vấn và CRUD chi tiết kiểm kê (có thể lọc độc lập theo phiếu/kho/SP).
/// </summary>
public class StocktakeDetailRepository : IStocktakeDetailRepository
{
    private readonly ApplicationDbContext _dbContext;

    public StocktakeDetailRepository(ApplicationDbContext db)
    {
        _dbContext = db;
    }

    /// <summary>Lấy chi tiết kiểm kê theo Id.</summary>
    public Task<StocktakeDetail?> GetByIdAsync(int id)
        => Query()
            .FirstOrDefaultAsync(x => x.Id == id);

    /// <summary>Lấy chi tiết theo Id thuộc đúng phiếu kiểm kê.</summary>
    public Task<StocktakeDetail?> GetByStocktakeAndIdAsync(int stocktakeId, int detailId)
        => Query()
            .FirstOrDefaultAsync(x => x.Id == detailId && x.StocktakeId == stocktakeId);

    /// <summary>Kiểm tra sản phẩm đã có trên phiếu (tránh trùng dòng).</summary>
    public async Task<bool> ExistsProductAsync(int stocktakeId, int productId, int? excludeDetailId = null)
    {
        var query = _dbContext.StocktakeDetails.AsNoTracking()
            .Where(x => x.StocktakeId == stocktakeId && x.ProductId == productId);

        if (excludeDetailId.HasValue)
            query = query.Where(x => x.Id != excludeDetailId.Value);

        return await query.AnyAsync();
    }

    /// <summary>
    /// Phân trang chi tiết KK; lọc phiếu/kho/SP/trạng thái/chỉ lệch; sort theo |Difference|.
    /// </summary>
    public async Task<PagedResult<StocktakeDetail>> GetPagedAsync(
        string? search,
        int? stocktakeId,
        int? warehouseId,
        int? productId,
        StocktakeStatus? status,
        bool? onlyDiff,
        int pageNumber,
        int pageSize)
    {
        var query = Query().AsNoTracking();

        if (stocktakeId is > 0)
            query = query.Where(x => x.StocktakeId == stocktakeId.Value);

        if (warehouseId is > 0)
            query = query.Where(x => x.Stocktake != null && x.Stocktake.WarehouseId == warehouseId.Value);

        if (productId is > 0)
            query = query.Where(x => x.ProductId == productId.Value);

        if (status.HasValue)
            query = query.Where(x => x.Stocktake != null && x.Stocktake.Status == status.Value);

        if (onlyDiff == true)
            query = query.Where(x => x.Difference != 0);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                (x.Product != null && (x.Product.Name.Contains(search) || x.Product.Sku.Contains(search))) ||
                (x.Stocktake != null && x.Stocktake.Code.Contains(search)) ||
                (x.Stocktake != null && x.Stocktake.Warehouse != null &&
                 (x.Stocktake.Warehouse.Name.Contains(search) || x.Stocktake.Warehouse.Code.Contains(search))) ||
                (x.Note != null && x.Note.Contains(search)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => Math.Abs(x.Difference))
            .ThenByDescending(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<StocktakeDetail>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = total
        };
    }

    /// <summary>Thêm chi tiết kiểm kê.</summary>
    public Task AddAsync(StocktakeDetail detail) => _dbContext.StocktakeDetails.AddAsync(detail).AsTask();

    /// <summary>Cập nhật chi tiết kiểm kê.</summary>
    public void Update(StocktakeDetail detail) => _dbContext.StocktakeDetails.Update(detail);

    /// <summary>Xóa chi tiết kiểm kê.</summary>
    public void Delete(StocktakeDetail detail) => _dbContext.StocktakeDetails.Remove(detail);

    /// <summary>Lưu thay đổi xuống DB.</summary>
    public Task SaveChangesAsync() => _dbContext.SaveChangesAsync();

    /// <summary>Query gốc: Include Stocktake → Warehouse, Product → Unit.</summary>
    private IQueryable<StocktakeDetail> Query()
        => _dbContext.StocktakeDetails
            .Include(x => x.Stocktake)!.ThenInclude(s => s!.Warehouse)
            .Include(x => x.Product)!.ThenInclude(p => p!.Unit);
}
