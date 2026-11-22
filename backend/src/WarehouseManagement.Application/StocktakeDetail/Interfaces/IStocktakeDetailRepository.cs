// Lớp Application — hợp đồng truy cập dữ liệu dòng chi tiết kiểm kê.
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Interfaces;

public interface IStocktakeDetailRepository
{
    // Lấy dòng chi tiết theo Id.
    Task<StocktakeDetail?> GetByIdAsync(int id);

    // Lấy dòng chi tiết thuộc đúng phiếu stocktakeId.
    Task<StocktakeDetail?> GetByStocktakeAndIdAsync(int stocktakeId, int detailId);

    // Kiểm tra sản phẩm đã có trên phiếu chưa (tránh trùng dòng).
    Task<bool> ExistsProductAsync(int stocktakeId, int productId, int? excludeDetailId = null);

    // Lấy danh sách chi tiết phân trang; lọc phiếu, kho, SP, trạng thái, chỉ lệch.
    Task<PagedResult<StocktakeDetail>> GetPagedAsync(
        string? search,
        int? stocktakeId,
        int? warehouseId,
        int? productId,
        StocktakeStatus? status,
        bool? onlyDiff,
        int pageNumber,
        int pageSize);

    // Thêm dòng chi tiết mới.
    Task AddAsync(StocktakeDetail detail);

    // Đánh dấu dòng chi tiết cần cập nhật.
    void Update(StocktakeDetail detail);

    // Đánh dấu dòng chi tiết cần xóa.
    void Delete(StocktakeDetail detail);

    // Lưu thay đổi xuống DB.
    Task SaveChangesAsync();
}
