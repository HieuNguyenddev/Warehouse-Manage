// Lớp Application — hợp đồng truy cập dữ liệu khách hàng.
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Interfaces;

public interface ICustomerRepository
{
    // Lấy Customer theo Id.
    Task<Customer?> GetByIdAsync(int id);

    // Lấy Customer theo UserId liên kết.
    Task<Customer?> GetByUserIdAsync(int userId);

    // Lấy danh sách Customer phân trang theo từ khóa.
    Task<PagedResult<Customer>> GetPagedAsync(string? search, int pageNumber, int pageSize);

    // Kiểm tra tên khách hàng đã tồn tại.
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null);

    // Kiểm tra email khách hàng đã tồn tại.
    Task<bool> ExistsByEmailAsync(string email, int? excludeId = null);

    // Kiểm tra mã số thuế đã tồn tại.
    Task<bool> ExistsByTaxCodeAsync(string taxCode, int? excludeId = null);

    // Thêm Customer mới.
    Task AddAsync(Customer customer);

    // Đánh dấu Customer cần cập nhật.
    void Update(Customer customer);

    // Đánh dấu Customer cần xóa.
    void Delete(Customer customer);

    // Lưu thay đổi xuống DB.
    Task SaveChangesAsync();
}
