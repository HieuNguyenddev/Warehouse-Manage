// Lớp Application — hợp đồng nghiệp vụ phiếu nhập kho (tạo, sửa, xác nhận, hủy).
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Interfaces;

public interface IImportReceiptService
{
    // Lấy phiếu nhập theo Id (kèm chi tiết).
    Task<ImportReceiptResponseDto> GetByIdAsync(int id);

    // Lấy danh sách phiếu nhập phân trang; lọc kho, NCC, trạng thái.
    Task<PagedResult<ImportReceiptResponseDto>> GetAllAsync(
        PaginationQuery query,
        int? warehouseId = null,
        int? supplierId = null,
        ImportReceiptStatus? status = null);

    // Tạo phiếu nhập ở trạng thái nháp (Draft).
    Task<ImportReceiptResponseDto> CreateAsync(ImportReceiptRequestDto dto);

    // Cập nhật phiếu nhập còn ở trạng thái nháp.
    Task<ImportReceiptResponseDto> UpdateAsync(int id, ImportReceiptRequestDto dto);

    // Xác nhận phiếu nhập: cộng tồn kho và ghi sổ giao dịch.
    Task<ImportReceiptResponseDto> ConfirmAsync(int id);

    // Hủy phiếu nhập (không áp dụng tồn nếu chưa confirm; quy tắc theo service).
    Task<ImportReceiptResponseDto> CancelAsync(int id);

    // Xóa phiếu nhập (thường chỉ khi còn Draft).
    Task DeleteAsync(int id);
}
