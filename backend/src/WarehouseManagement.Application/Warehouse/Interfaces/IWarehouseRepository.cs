// Lớp Application — hợp đồng truy cập dữ liệu kho hàng.
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Interfaces;

public interface IWarehouseRepository
{
    // Lấy Warehouse theo Id.
    Task<Warehouse?> GetByIdAsync(int id);

    // Lấy danh sách Warehouse phân trang theo từ khóa.
    Task<PagedResult<Warehouse>> GetPagedAsync(string? search, int pageNumber, int pageSize);

    // Kiểm tra mã kho đã tồn tại.
    Task<bool> ExistsByCodeAsync(string code, int? excludeId = null);

    // Kiểm tra tên kho đã tồn tại.
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null);

    // Thêm Warehouse mới.
    Task AddAsync(Warehouse warehouse);

    // Đánh dấu Warehouse cần cập nhật.
    void Update(Warehouse warehouse);

    // Đánh dấu Warehouse cần xóa.
    void Delete(Warehouse warehouse);

    // Lưu thay đổi xuống DB.
    Task SaveChangesAsync();
}
