// Lớp Application — hợp đồng truy cập dữ liệu tồn kho theo kho–sản phẩm.
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Interfaces;

public interface IWarehouseStockRepository
{
    // Lấy dòng tồn theo Id.
    Task<WarehouseStock?> GetByIdAsync(int id);

    // Lấy dòng tồn theo cặp kho–sản phẩm.
    Task<WarehouseStock?> GetByWarehouseAndProductAsync(int warehouseId, int productId);

    // Lấy danh sách tồn trong một kho (giới hạn take).
    Task<List<WarehouseStock>> GetListByWarehouseAsync(int warehouseId, int take = 1000);

    // Lấy tồn theo danh sách productId (mọi kho hoặc theo ngữ cảnh repo).
    Task<List<WarehouseStock>> GetByProductIdsAsync(IEnumerable<int> productIds);

    // Lấy tồn phân trang; lọc search, kho, SP, tồn thấp / hết hàng.
    Task<PagedResult<WarehouseStock>> GetPagedAsync(
        string? search,
        int? warehouseId,
        int? productId,
        bool? lowStockOnly,
        bool? outOfStockOnly,
        int pageNumber,
        int pageSize);

    // Tổng hợp số SKU, tồn thấp, hết hàng, tổng số lượng (có thể theo kho).
    Task<(int TotalSkus, int LowStockCount, int OutOfStockCount, decimal TotalQuantity)> GetSummaryAsync(
        int? warehouseId = null);

    // Thêm dòng tồn mới.
    Task AddAsync(WarehouseStock stock);

    // Đánh dấu dòng tồn cần cập nhật.
    void Update(WarehouseStock stock);

    // Lưu thay đổi xuống DB.
    Task SaveChangesAsync();
}
