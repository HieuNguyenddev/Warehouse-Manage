using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Infrastructure.Persistence;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Infrastructure.Repositories;

public class StockLotRepository : IStockLotRepository
{
    private readonly ApplicationDbContext _db;

    public StockLotRepository(ApplicationDbContext db) => _db = db;

    public Task<StockLot?> GetByKeyAsync(int warehouseId, int productId, string lotNumber)
        => _db.StockLots.FirstOrDefaultAsync(x =>
            x.WarehouseId == warehouseId
            && x.ProductId == productId
            && x.LotNumber == lotNumber);

    public Task<List<StockLot>> GetAvailableLotsAsync(int warehouseId, int productId)
        => _db.StockLots
            .Where(x => x.WarehouseId == warehouseId && x.ProductId == productId && x.Quantity > 0)
            .OrderBy(x => x.ExpiryDate ?? DateTime.MaxValue)
            .ThenBy(x => x.Id)
            .ToListAsync();

    public async Task<PagedResult<StockLot>> GetNearExpiryAsync(
        int withinDays,
        int? warehouseId,
        int pageNumber,
        int pageSize)
    {
        var today = DateTime.UtcNow.Date;
        var until = today.AddDays(withinDays);

        var q = _db.StockLots.AsNoTracking()
            .Include(x => x.Warehouse)
            .Include(x => x.Product)!.ThenInclude(p => p!.Unit)
            .Where(x => x.Quantity > 0 && x.ExpiryDate != null && x.ExpiryDate <= until);

        if (warehouseId is > 0)
            q = q.Where(x => x.WarehouseId == warehouseId);

        q = q.OrderBy(x => x.ExpiryDate).ThenBy(x => x.Product!.Sku);

        var total = await q.CountAsync();
        var items = await q.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResult<StockLot>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = total
        };
    }

    public async Task<PagedResult<StockLot>> GetPagedAsync(
        string? search,
        int? warehouseId,
        int? productId,
        int pageNumber,
        int pageSize)
    {
        var q = _db.StockLots.AsNoTracking()
            .Include(x => x.Warehouse)
            .Include(x => x.Product)!.ThenInclude(p => p!.Unit)
            .Where(x => x.Quantity > 0);

        if (warehouseId is > 0)
            q = q.Where(x => x.WarehouseId == warehouseId);
        if (productId is > 0)
            q = q.Where(x => x.ProductId == productId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(x =>
                x.LotNumber.Contains(s)
                || (x.Product != null && (x.Product.Sku.Contains(s) || x.Product.Name.Contains(s)))
                || (x.Warehouse != null && (x.Warehouse.Code.Contains(s) || x.Warehouse.Name.Contains(s))));
        }

        q = q.OrderBy(x => x.ExpiryDate ?? DateTime.MaxValue).ThenBy(x => x.Product!.Sku);

        var total = await q.CountAsync();
        var items = await q.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResult<StockLot>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = total
        };
    }

    public Task AddAsync(StockLot lot) => _db.StockLots.AddAsync(lot).AsTask();

    public void Update(StockLot lot) => _db.StockLots.Update(lot);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
