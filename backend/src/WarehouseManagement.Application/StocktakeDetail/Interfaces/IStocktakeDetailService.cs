// Lớp Application — hợp đồng nghiệp vụ dòng chi tiết kiểm kê (báo cáo + CRUD trên Draft).
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Interfaces;

public interface IStocktakeDetailService
{
    // Báo cáo / danh sách chi tiết + chênh lệch; lọc phiếu, kho, SP, trạng thái, chỉ lệch.
    Task<PagedResult<StocktakeDetailItemDto>> GetAllAsync(
        PaginationQuery query,
        int? stocktakeId = null,
        int? warehouseId = null,
        int? productId = null,
        StocktakeStatus? status = null,
        bool? onlyDiff = null);

    // Lấy một dòng chi tiết kiểm kê theo Id.
    Task<StocktakeDetailItemDto> GetByIdAsync(int id);

    // Thêm dòng sản phẩm vào phiếu kiểm kê còn Draft.
    Task<StocktakeDetailItemDto> AddAsync(int stocktakeId, StocktakeDetailRequestDto dto);

    // Cập nhật số lượng thực tế / ghi chú của dòng trên phiếu Draft.
    Task<StocktakeDetailItemDto> UpdateAsync(int stocktakeId, int detailId, StocktakeDetailUpdateDto dto);

    // Xóa dòng chi tiết khỏi phiếu Draft.
    Task DeleteAsync(int stocktakeId, int detailId);
}
