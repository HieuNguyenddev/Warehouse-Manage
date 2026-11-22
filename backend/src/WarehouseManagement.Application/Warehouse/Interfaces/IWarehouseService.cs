// Lớp Application — hợp đồng nghiệp vụ quản lý kho hàng.
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Interfaces;

public interface IWarehouseService
{
    // Lấy kho theo Id.
    Task<WarehouseResponseDto> GetByIdAsync(int id);

    // Lấy danh sách kho phân trang.
    Task<PagedResult<WarehouseResponseDto>> GetAllAsync(PaginationQuery query);

    // Tạo kho mới.
    Task<WarehouseResponseDto> CreateAsync(WarehouseRequestDto dto);

    // Cập nhật thông tin kho.
    Task<WarehouseResponseDto> UpdateAsync(int id, WarehouseRequestDto dto);

    // Xóa kho theo Id.
    Task DeleteAsync(int id);
}
