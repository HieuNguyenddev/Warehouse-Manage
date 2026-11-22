// Lớp Application — hợp đồng truy cập dữ liệu hóa đơn bán hàng.
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Interfaces;

public interface IInvoiceRepository
{
    // Lấy hóa đơn theo Id.
    Task<Invoice?> GetByIdAsync(int id);

    // Lấy hóa đơn theo Id chỉ khi thuộc về customerId.
    Task<Invoice?> GetByIdForCustomerAsync(int id, int customerId);

    // Lấy danh sách hóa đơn phân trang; có thể lọc theo khách hàng.
    Task<PagedResult<Invoice>> GetPagedAsync(string? search, int? customerId, int pageNumber, int pageSize);

    // Đếm số hóa đơn có mã bắt đầu bằng prefix (dùng sinh mã tự động).
    Task<int> CountByCodePrefixAsync(string prefix);

    // Thêm hóa đơn mới.
    Task AddAsync(Invoice invoice);

    // Lưu thay đổi xuống DB.
    Task SaveChangesAsync();
}
