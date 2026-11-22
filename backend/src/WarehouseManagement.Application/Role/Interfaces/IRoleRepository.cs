// Lớp Application — hợp đồng truy cập dữ liệu vai trò (Role).
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Interfaces;

public interface IRoleRepository
{
    // Lấy Role theo Id.
    Task<Role?> GetByIdAsync(int id);

    // Lấy Role theo tên.
    Task<Role?> GetByNameAsync(string name);

    // Lấy danh sách Role phân trang theo từ khóa.
    Task<PagedResult<Role>> GetPagedAsync(string? search, int pageNumber, int pageSize);

    // Kiểm tra tên Role đã tồn tại (có thể loại trừ Id khi cập nhật).
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null);

    // Thêm Role mới.
    Task AddAsync(Role role);

    // Đánh dấu Role cần cập nhật.
    void Update(Role role);

    // Đánh dấu Role cần xóa.
    void Delete(Role role);

    // Lưu thay đổi xuống DB.
    Task SaveChangesAsync();
}
