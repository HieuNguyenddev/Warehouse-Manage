// Lớp Application — hợp đồng truy cập dữ liệu phiếu xuất kho.
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Interfaces;

public interface IExportReceiptRepository
{
    // Lấy phiếu xuất theo Id (kèm chi tiết).
    Task<ExportReceipt?> GetByIdAsync(int id);

    // Lấy danh sách phiếu xuất phân trang; lọc search, kho, KH, trạng thái, nguồn.
    Task<PagedResult<ExportReceipt>> GetPagedAsync(
        string? search,
        int? warehouseId,
        int? customerId,
        ExportReceiptStatus? status,
        ExportReceiptSource? source,
        int pageNumber,
        int pageSize);

    // Đếm số phiếu có mã bắt đầu bằng prefix (dùng sinh mã tự động).
    Task<int> CountByCodePrefixAsync(string prefix);

    // Thêm phiếu xuất mới.
    Task AddAsync(ExportReceipt receipt);

    // Đánh dấu phiếu xuất cần cập nhật.
    void Update(ExportReceipt receipt);

    // Đánh dấu phiếu xuất cần xóa.
    void Delete(ExportReceipt receipt);

    // Xóa hàng loạt dòng chi tiết phiếu.
    void RemoveDetails(IEnumerable<ExportReceiptDetail> details);

    // Lưu thay đổi xuống DB.
    Task SaveChangesAsync();
}
