// Lớp Application — hợp đồng nghiệp vụ phiếu xuất kho (tạo, sửa, xác nhận, hủy).
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Interfaces;

public interface IExportReceiptService
{
    // Lấy phiếu xuất theo Id (kèm chi tiết).
    Task<ExportReceiptResponseDto> GetByIdAsync(int id);

    // Lấy danh sách phiếu xuất phân trang; lọc kho, KH, trạng thái, nguồn.
    Task<PagedResult<ExportReceiptResponseDto>> GetAllAsync(
        PaginationQuery query,
        int? warehouseId = null,
        int? customerId = null,
        ExportReceiptStatus? status = null,
        ExportReceiptSource? source = null);

    // Tạo phiếu xuất ở trạng thái nháp (Draft).
    Task<ExportReceiptResponseDto> CreateAsync(ExportReceiptRequestDto dto);

    // Cập nhật phiếu xuất còn ở trạng thái nháp.
    Task<ExportReceiptResponseDto> UpdateAsync(int id, ExportReceiptRequestDto dto);

    // Xác nhận phiếu xuất: trừ tồn kho và ghi sổ giao dịch.
    Task<ExportReceiptResponseDto> ConfirmAsync(int id);

    // Hủy phiếu xuất theo quy tắc trạng thái trong service.
    Task<ExportReceiptResponseDto> CancelAsync(int id);

    // Xóa phiếu xuất (thường chỉ khi còn Draft).
    Task DeleteAsync(int id);
}
