// Lớp Application — hợp đồng truy cập dữ liệu phiếu kiểm kê.
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Interfaces;

public interface IStocktakeRepository
{
    // Lấy phiếu kiểm kê theo Id (kèm chi tiết).
    Task<Stocktake?> GetByIdAsync(int id);

    // Lấy danh sách phiếu kiểm kê phân trang; lọc search, kho, trạng thái.
    Task<PagedResult<Stocktake>> GetPagedAsync(
        string? search,
        int? warehouseId,
        StocktakeStatus? status,
        int pageNumber,
        int pageSize);

    // Đếm số phiếu có mã bắt đầu bằng prefix (dùng sinh mã tự động).
    Task<int> CountByCodePrefixAsync(string prefix);

    // Thêm phiếu kiểm kê mới.
    Task AddAsync(Stocktake stocktake);

    // Đánh dấu phiếu kiểm kê cần cập nhật.
    void Update(Stocktake stocktake);

    // Đánh dấu phiếu kiểm kê cần xóa.
    void Delete(Stocktake stocktake);

    // Xóa hàng loạt dòng chi tiết kiểm kê.
    void RemoveDetails(IEnumerable<StocktakeDetail> details);

    // Lưu thay đổi xuống DB.
    Task SaveChangesAsync();
}
