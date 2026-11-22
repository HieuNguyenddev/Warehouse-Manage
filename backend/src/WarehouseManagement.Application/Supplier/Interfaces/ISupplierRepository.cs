// Lớp Application — hợp đồng truy cập dữ liệu nhà cung cấp.
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Interfaces;

public interface ISupplierRepository
{
    // Lấy Supplier theo Id.
    Task<Supplier?> GetByIdAsync(int id);

    // Lấy danh sách Supplier phân trang theo từ khóa.
    Task<PagedResult<Supplier>> GetPagedAsync(string? search, int pageNumber, int pageSize);

    // Kiểm tra tên nhà cung cấp đã tồn tại.
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null);

    // Kiểm tra email nhà cung cấp đã tồn tại.
    Task<bool> ExistsByEmailAsync(string email, int? excludeId = null);

    // Kiểm tra mã số thuế đã tồn tại.
    Task<bool> ExistsByTaxCodeAsync(string taxCode, int? excludeId = null);

    // Thêm Supplier mới.
    Task AddAsync(Supplier supplier);

    // Đánh dấu Supplier cần cập nhật.
    void Update(Supplier supplier);

    // Đánh dấu Supplier cần xóa.
    void Delete(Supplier supplier);

    // Lưu thay đổi xuống DB.
    Task SaveChangesAsync();
}
