// Lớp Application — hợp đồng truy cập dữ liệu phiếu nhập kho.
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Interfaces;

public interface IImportReceiptRepository
{
    // Lấy phiếu nhập theo Id (kèm chi tiết).
    Task<ImportReceipt?> GetByIdAsync(int id);

    // Lấy danh sách phiếu nhập phân trang; lọc search, kho, NCC, trạng thái.
    Task<PagedResult<ImportReceipt>> GetPagedAsync(
        string? search,
        int? warehouseId,
        int? supplierId,
        ImportReceiptStatus? status,
        int pageNumber,
        int pageSize);

    // Đếm số phiếu có mã bắt đầu bằng prefix (dùng sinh mã tự động).
    Task<int> CountByCodePrefixAsync(string prefix);

    // Thêm phiếu nhập mới.
    Task AddAsync(ImportReceipt receipt);

    // Đánh dấu phiếu nhập cần cập nhật.
    void Update(ImportReceipt receipt);

    // Đánh dấu phiếu nhập cần xóa.
    void Delete(ImportReceipt receipt);

    // Xóa hàng loạt dòng chi tiết phiếu.
    void RemoveDetails(IEnumerable<ImportReceiptDetail> details);

    // Lưu thay đổi xuống DB.
    Task SaveChangesAsync();
}
