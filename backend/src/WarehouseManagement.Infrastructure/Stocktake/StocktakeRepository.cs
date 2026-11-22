using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Infrastructure.Persistence;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Infrastructure.Repositories;

/// <summary>
/// Truy vấn và CRUD phiếu kiểm kê + chi tiết.
/// </summary>
public class StocktakeRepository : IStocktakeRepository
{
    private readonly ApplicationDbContext _dbContext;

    public StocktakeRepository(ApplicationDbContext db)
    {
        _dbContext = db;
    }

    /// <summary>Lấy phiếu kiểm kê theo Id kèm Warehouse, Details → Product → Unit.</summary>
    public Task<Stocktake?> GetByIdAsync(int id)
        => _dbContext.Stocktakes
            .Include(x => x.Warehouse)
            .Include(x => x.Details)
                .ThenInclude(d => d.Product)!
                .ThenInclude(p => p!.Unit)
            .FirstOrDefaultAsync(x => x.Id == id);

    /// <summary>Phân trang kiểm kê; lọc theo kho, trạng thái và search (mã/ghi chú/kho).</summary>
    public async Task<PagedResult<Stocktake>> GetPagedAsync(
        string? search,
        int? warehouseId,
        StocktakeStatus? status,
        int pageNumber,
        int pageSize)
    {
        var query = _dbContext.Stocktakes
            .AsNoTracking()
            .Include(x => x.Warehouse)
            .Include(x => x.Details)
                .ThenInclude(d => d.Product)!
                .ThenInclude(p => p!.Unit)
            .AsQueryable();

        if (warehouseId is > 0)
            query = query.Where(x => x.WarehouseId == warehouseId.Value);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                x.Code.Contains(search) ||
                (x.Note != null && x.Note.Contains(search)) ||
                (x.Warehouse != null && (x.Warehouse.Name.Contains(search) || x.Warehouse.Code.Contains(search))));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Stocktake>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = total
        };
    }

    /// <summary>Đếm số phiếu có mã bắt đầu bằng prefix (sinh mã tự động).</summary>
    public Task<int> CountByCodePrefixAsync(string prefix)
        => _dbContext.Stocktakes.AsNoTracking().CountAsync(x => x.Code.StartsWith(prefix));

    /// <summary>Thêm phiếu kiểm kê mới.</summary>
    public Task AddAsync(Stocktake stocktake) => _dbContext.Stocktakes.AddAsync(stocktake).AsTask();

    /// <summary>Cập nhật phiếu kiểm kê.</summary>
    public void Update(Stocktake stocktake) => _dbContext.Stocktakes.Update(stocktake);

    /// <summary>Xóa phiếu kiểm kê.</summary>
    public void Delete(Stocktake stocktake) => _dbContext.Stocktakes.Remove(stocktake);

    /// <summary>Xóa hàng loạt chi tiết kiểm kê.</summary>
    public void RemoveDetails(IEnumerable<StocktakeDetail> details)
        => _dbContext.StocktakeDetails.RemoveRange(details);

    /// <summary>Lưu thay đổi xuống DB.</summary>
    public Task SaveChangesAsync() => _dbContext.SaveChangesAsync();
}
