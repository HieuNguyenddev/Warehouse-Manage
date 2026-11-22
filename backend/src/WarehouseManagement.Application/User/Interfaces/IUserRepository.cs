// Lớp Application — hợp đồng truy cập dữ liệu người dùng (persistence abstraction).
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Interfaces;

public interface IUserRepository
{
    // Lấy User theo Id (kèm Role nếu có).
    Task<User?> GetByIdAsync(int id);

    // Lấy User theo tên đăng nhập.
    Task<User?> GetByUsernameAsync(string username);

    // Lấy danh sách User phân trang theo từ khóa tìm kiếm.
    Task<PagedResult<User>> GetPagedAsync(string? search, int pageNumber, int pageSize);

    // Kiểm tra username đã tồn tại (có thể loại trừ một Id khi cập nhật).
    Task<bool> ExistsByUsernameAsync(string username, int? excludeId = null);

    // Kiểm tra email đã tồn tại (có thể loại trừ một Id khi cập nhật).
    Task<bool> ExistsByEmailAsync(string email, int? excludeId = null);

    // Thêm User mới vào ngữ cảnh.
    Task AddAsync(User user);

    // Đánh dấu User cần cập nhật.
    void Update(User user);

    // Đánh dấu User cần xóa.
    void Delete(User user);

    // Lưu thay đổi xuống cơ sở dữ liệu.
    Task SaveChangesAsync();
}
