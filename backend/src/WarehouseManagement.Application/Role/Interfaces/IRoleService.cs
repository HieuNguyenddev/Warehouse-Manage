// Lớp Application — hợp đồng nghiệp vụ quản lý vai trò (Role).
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Interfaces;

public interface IRoleService
{
    // Lấy vai trò theo Id.
    Task<RoleResponseDto> GetByIdAsync(int id);

    // Lấy danh sách vai trò phân trang.
    Task<PagedResult<RoleResponseDto>> GetAllAsync(PaginationQuery query);

    // Tạo vai trò mới.
    Task<RoleResponseDto> CreateAsync(RoleRequestDto dto);

    // Cập nhật vai trò.
    Task<RoleResponseDto> UpdateAsync(int id, RoleRequestDto dto);

    // Xóa vai trò theo Id.
    Task DeleteAsync(int id);
}
