// Lớp Application — hợp đồng nghiệp vụ phiếu kiểm kê kho.
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Interfaces;

public interface IStocktakeService
{
    // Lấy phiếu kiểm kê theo Id (kèm chi tiết).
    Task<StocktakeResponseDto> GetByIdAsync(int id);

    // Lấy danh sách phiếu kiểm kê phân trang; lọc kho, trạng thái.
    Task<PagedResult<StocktakeResponseDto>> GetAllAsync(
        PaginationQuery query,
        int? warehouseId = null,
        StocktakeStatus? status = null);

    // Tạo phiếu kiểm kê thủ công (nhập chi tiết).
    Task<StocktakeResponseDto> CreateAsync(StocktakeRequestDto dto);

    // Tạo phiếu kiểm kê tự động từ tồn hiện tại của kho.
    Task<StocktakeResponseDto> CreateAutoAsync(AutoStocktakeRequestDto dto);

    // Cập nhật phiếu kiểm kê còn Draft.
    Task<StocktakeResponseDto> UpdateAsync(int id, StocktakeRequestDto dto);

    // Xác nhận kiểm kê: áp dụng chênh lệch tồn và ghi sổ.
    Task<StocktakeResponseDto> ConfirmAsync(int id);

    // Hủy phiếu kiểm kê.
    Task<StocktakeResponseDto> CancelAsync(int id);

    // Xóa phiếu kiểm kê (thường chỉ khi Draft).
    Task DeleteAsync(int id);
}
