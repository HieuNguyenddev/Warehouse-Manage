// Lớp Application — hợp đồng nghiệp vụ tồn kho theo kho–sản phẩm.
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Interfaces;

public interface IWarehouseStockService
{
    // Lấy dòng tồn kho theo Id.
    Task<WarehouseStockResponseDto> GetByIdAsync(int id);

    // Lấy danh sách tồn kho phân trang; lọc theo kho, sản phẩm, tồn thấp / hết hàng.
    Task<PagedResult<WarehouseStockResponseDto>> GetAllAsync(
        PaginationQuery query,
        int? warehouseId = null,
        int? productId = null,
        bool? lowStockOnly = null,
        bool? outOfStockOnly = null);

    // Lấy tổng quan tồn kho (số SKU, tồn thấp, hết hàng, tổng số lượng).
    Task<WarehouseStockSummaryDto> GetSummaryAsync(int? warehouseId = null);

    // Khởi tạo tồn đầu kỳ (chỉ khi chưa có dòng tồn cho cặp kho–SP).
    Task<WarehouseStockResponseDto> CreateOpeningAsync(WarehouseStockRequestDto dto);

    // Điều chỉnh tồn tuyệt đối kèm lý do (ghi sổ giao dịch Manual).
    Task<WarehouseStockResponseDto> AdjustAsync(int id, WarehouseStockAdjustDto dto);

    // Cập nhật ngưỡng tồn tối thiểu theo kho.
    Task<WarehouseStockResponseDto> UpdateMinAsync(int id, WarehouseStockMinDto dto);

    // Tương thích API cũ — tạo tồn (ủy quyền CreateOpeningAsync).
    Task<WarehouseStockResponseDto> CreateAsync(WarehouseStockRequestDto dto);

    // Tương thích API cũ — cập nhật tồn (map sang Adjust/UpdateMin tùy DTO).
    Task<WarehouseStockResponseDto> UpdateAsync(int id, WarehouseStockUpdateDto dto);
}
