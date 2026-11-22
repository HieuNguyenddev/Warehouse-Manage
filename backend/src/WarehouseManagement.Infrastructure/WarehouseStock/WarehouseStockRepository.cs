using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Infrastructure.Persistence;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Infrastructure.Repositories;

/// <summary>
/// Truy vấn tồn kho theo kho + sản phẩm; hỗ trợ lọc tồn thấp / hết hàng và tóm tắt.
/// </summary>
public class WarehouseStockRepository : IWarehouseStockRepository
{
    private readonly ApplicationDbContext _dbContext;

    public WarehouseStockRepository(ApplicationDbContext db)
    {
        _dbContext = db;
    }

    /// <summary>Lấy dòng tồn theo Id kèm Warehouse, Product (Unit, Category).</summary>
    public Task<WarehouseStock?> GetByIdAsync(int id)
        => _dbContext.WarehouseStocks
            .Include(x => x.Warehouse)
            .Include(x => x.Product)!.ThenInclude(p => p!.Unit)
            .Include(x => x.Product)!.ThenInclude(p => p!.Category)
            .FirstOrDefaultAsync(x => x.Id == id);

    /// <summary>Lấy tồn theo cặp kho + sản phẩm (tracked — dùng khi cộng/trừ tồn).</summary>
    public Task<WarehouseStock?> GetByWarehouseAndProductAsync(int warehouseId, int productId)
        => _dbContext.WarehouseStocks
            .FirstOrDefaultAsync(x => x.WarehouseId == warehouseId && x.ProductId == productId);

    /// <summary>Danh sách tồn trong 1 kho (SP đang active), sắp theo SKU.</summary>
    public Task<List<WarehouseStock>> GetListByWarehouseAsync(int warehouseId, int take = 1000)
        => _dbContext.WarehouseStocks
            .AsNoTracking()
            .Include(x => x.Product)
            .Where(x => x.WarehouseId == warehouseId && x.Product != null && x.Product.IsActive)
            .OrderBy(x => x.Product!.Sku)
            .Take(take)
            .ToListAsync();

    /// <summary>Lấy tồn theo danh sách ProductId; chỉ kho active và Quantity &gt; 0.</summary>
    public Task<List<WarehouseStock>> GetByProductIdsAsync(IEnumerable<int> productIds)
    {
        var ids = productIds.Distinct().ToList();
        return _dbContext.WarehouseStocks
            .AsNoTracking()
            .Include(x => x.Warehouse)
            .Include(x => x.Product)
            .Where(x =>
                ids.Contains(x.ProductId) &&
                x.Warehouse != null &&
                x.Warehouse.IsActive &&
                x.Quantity > 0)
            .ToListAsync();
    }

    /// <summary>
    /// Phân trang tồn kho; lọc theo search, kho, SP, chỉ tồn thấp / hết hàng.
    /// </summary>
    public async Task<PagedResult<WarehouseStock>> GetPagedAsync(
        string? search,
        int? warehouseId,
        int? productId,
        bool? lowStockOnly,
        bool? outOfStockOnly,
        int pageNumber,
        int pageSize)
    {
        var query = BuildFilteredQuery(search, warehouseId, productId, lowStockOnly, outOfStockOnly);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.Warehouse!.Code)
            .ThenBy(x => x.Product!.Sku)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<WarehouseStock>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = total
        };
    }

    /// <summary>
    /// Tóm tắt: số SKU, số dòng tồn thấp, hết hàng, tổng số lượng (lọc theo kho tùy chọn).
    /// </summary>
    public async Task<(int TotalSkus, int LowStockCount, int OutOfStockCount, decimal TotalQuantity)> GetSummaryAsync(
        int? warehouseId = null)
    {
        var baseQuery = _dbContext.WarehouseStocks.AsNoTracking().AsQueryable();
        if (warehouseId is > 0)
            baseQuery = baseQuery.Where(x => x.WarehouseId == warehouseId.Value);

        var totalSkus = await baseQuery.CountAsync();
        var totalQuantity = await baseQuery.SumAsync(x => (decimal?)x.Quantity) ?? 0m;
        var outOfStock = await baseQuery.CountAsync(x => x.Quantity <= 0);

        // Min hiệu lực: MinQuantity ?? Product.MinStock; chỉ cảnh báo khi min > 0
        var lowStock = await baseQuery.CountAsync(x =>
            x.Product != null &&
            ((x.MinQuantity ?? x.Product.MinStock) > 0) &&
            x.Quantity <= (x.MinQuantity ?? x.Product.MinStock));

        return (totalSkus, lowStock, outOfStock, totalQuantity);
    }

    /// <summary>Xây query lọc tồn (search / kho / SP / low / out-of-stock).</summary>
    private IQueryable<WarehouseStock> BuildFilteredQuery(
        string? search,
        int? warehouseId,
        int? productId,
        bool? lowStockOnly,
        bool? outOfStockOnly)
    {
        var query = _dbContext.WarehouseStocks
            .AsNoTracking()
            .Include(x => x.Warehouse)
            .Include(x => x.Product)!.ThenInclude(p => p!.Unit)
            .Include(x => x.Product)!.ThenInclude(p => p!.Category)
            .AsQueryable();

        if (warehouseId is > 0)
            query = query.Where(x => x.WarehouseId == warehouseId.Value);

        if (productId is > 0)
            query = query.Where(x => x.ProductId == productId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                (x.Product != null && (x.Product.Name.Contains(search) || x.Product.Sku.Contains(search))) ||
                (x.Warehouse != null && (x.Warehouse.Name.Contains(search) || x.Warehouse.Code.Contains(search))));
        }

        if (outOfStockOnly == true)
            query = query.Where(x => x.Quantity <= 0);
        else if (lowStockOnly == true)
        {
            query = query.Where(x =>
                x.Product != null &&
                ((x.MinQuantity ?? x.Product.MinStock) > 0) &&
                x.Quantity <= (x.MinQuantity ?? x.Product.MinStock));
        }

        return query;
    }

    /// <summary>Thêm dòng tồn mới.</summary>
    public Task AddAsync(WarehouseStock stock) => _dbContext.WarehouseStocks.AddAsync(stock).AsTask();

    /// <summary>Cập nhật dòng tồn.</summary>
    public void Update(WarehouseStock stock) => _dbContext.WarehouseStocks.Update(stock);

    /// <summary>Lưu thay đổi xuống DB.</summary>
    public Task SaveChangesAsync() => _dbContext.SaveChangesAsync();
}
