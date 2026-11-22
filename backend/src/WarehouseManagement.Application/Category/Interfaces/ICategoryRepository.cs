// Lớp Application — hợp đồng truy cập dữ liệu danh mục sản phẩm.
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Interfaces;

public interface ICategoryRepository
{
    // Lấy Category theo Id.
    Task<Category?> GetByIdAsync(int id);

    // Lấy danh sách Category phân trang theo từ khóa.
    Task<PagedResult<Category>> GetPagedAsync(string? search, int pageNumber, int pageSize);

    // Kiểm tra tên danh mục đã tồn tại (có thể loại trừ Id khi cập nhật).
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null);

    // Thêm Category mới.
    Task AddAsync(Category category);

    // Đánh dấu Category cần cập nhật.
    void Update(Category category);

    // Đánh dấu Category cần xóa.
    void Delete(Category category);

    // Lưu thay đổi xuống DB.
    Task SaveChangesAsync();
}
