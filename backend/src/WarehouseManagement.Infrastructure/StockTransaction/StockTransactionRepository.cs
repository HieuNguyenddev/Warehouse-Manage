using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Infrastructure.Persistence;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Infrastructure.Repositories;

/// <summary>
/// Truy vấn và ghi lịch sử biến động tồn kho (StockTransactions).
/// </summary>
public class StockTransactionRepository : IStockTransactionRepository
{
    private readonly ApplicationDbContext _dbContext;

    public StockTransactionRepository(ApplicationDbContext db)
    {
        _dbContext = db;
    }

    /// <summary>Lấy giao dịch tồn theo Id kèm Warehouse, Product → Unit.</summary>
    public Task<StockTransaction?> GetByIdAsync(int id)
        => Query().FirstOrDefaultAsync(x => x.Id == id);

    /// <summary>
    /// Phân trang lịch sử tồn; lọc kho/SP/loại/chứng từ/khoảng ngày và search.
    /// </summary>
    public async Task<PagedResult<StockTransaction>> GetPagedAsync(
        string? search,
        int? warehouseId,
        int? productId,
        StockTransactionType? type,
        string? referenceType,
        int? referenceId,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize)
    {
        var query = Query().AsNoTracking();

        if (warehouseId is > 0)
            query = query.Where(x => x.WarehouseId == warehouseId.Value);

        if (productId is > 0)
            query = query.Where(x => x.ProductId == productId.Value);

        if (type.HasValue)
            query = query.Where(x => x.Type == type.Value);

        if (!string.IsNullOrWhiteSpace(referenceType))
            query = query.Where(x => x.ReferenceType == referenceType);

        if (referenceId is > 0)
            query = query.Where(x => x.ReferenceId == referenceId.Value);

        if (fromDate.HasValue)
            query = query.Where(x => x.CreatedAt >= fromDate.Value.ToUniversalTime());

        if (toDate.HasValue)
            query = query.Where(x => x.CreatedAt <= toDate.Value.ToUniversalTime());

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                (x.ReferenceCode != null && x.ReferenceCode.Contains(search)) ||
                (x.Note != null && x.Note.Contains(search)) ||
                (x.Product != null && (x.Product.Name.Contains(search) || x.Product.Sku.Contains(search))) ||
                (x.Warehouse != null && (x.Warehouse.Name.Contains(search) || x.Warehouse.Code.Contains(search))));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<StockTransaction>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = total
        };
    }

    /// <summary>Thêm một giao dịch tồn.</summary>
    public Task AddAsync(StockTransaction transaction)
        => _dbContext.StockTransactions.AddAsync(transaction).AsTask();

    /// <summary>Thêm hàng loạt giao dịch tồn.</summary>
    public Task AddRangeAsync(IEnumerable<StockTransaction> transactions)
    {
        _dbContext.StockTransactions.AddRange(transactions);
        return Task.CompletedTask;
    }

    /// <summary>Lưu thay đổi xuống DB.</summary>
    public Task SaveChangesAsync() => _dbContext.SaveChangesAsync();

    /// <summary>Query gốc: Include Warehouse, Product → Unit.</summary>
    private IQueryable<StockTransaction> Query()
        => _dbContext.StockTransactions
            .Include(x => x.Warehouse)
            .Include(x => x.Product)!.ThenInclude(p => p!.Unit);
}
