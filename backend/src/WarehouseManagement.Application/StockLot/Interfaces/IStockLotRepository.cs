using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Interfaces;

public interface IStockLotRepository
{
    Task<StockLot?> GetByKeyAsync(int warehouseId, int productId, string lotNumber);
    Task<List<StockLot>> GetAvailableLotsAsync(int warehouseId, int productId);
    Task<PagedResult<StockLot>> GetNearExpiryAsync(
        int withinDays,
        int? warehouseId,
        int pageNumber,
        int pageSize);
    Task<PagedResult<StockLot>> GetPagedAsync(
        string? search,
        int? warehouseId,
        int? productId,
        int pageNumber,
        int pageSize);
    Task AddAsync(StockLot lot);
    void Update(StockLot lot);
    Task SaveChangesAsync();
}
