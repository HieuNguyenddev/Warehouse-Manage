// Lớp Application — hợp đồng truy cập dữ liệu đơn vị tính.
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Interfaces;

public interface IUnitRepository
{
    // Lấy Unit theo Id.
    Task<Unit?> GetByIdAsync(int id);

    // Lấy danh sách Unit phân trang theo từ khóa.
    Task<PagedResult<Unit>> GetPagedAsync(string? search, int pageNumber, int pageSize);

    // Kiểm tra tên đơn vị đã tồn tại (có thể loại trừ Id khi cập nhật).
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null);

    // Kiểm tra ký hiệu đơn vị đã tồn tại (có thể loại trừ Id khi cập nhật).
    Task<bool> ExistsBySymbolAsync(string symbol, int? excludeId = null);

    // Thêm Unit mới.
    Task AddAsync(Unit unit);

    // Đánh dấu Unit cần cập nhật.
    void Update(Unit unit);

    // Đánh dấu Unit cần xóa.
    void Delete(Unit unit);

    // Lưu thay đổi xuống DB.
    Task SaveChangesAsync();
}
