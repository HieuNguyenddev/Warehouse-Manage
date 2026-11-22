// Lớp Application — hợp đồng nghiệp vụ quản lý người dùng hệ thống.
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Interfaces;

public interface IUserService
{
    // Lấy thông tin người dùng theo Id.
    Task<UserResponseDto> GetByIdAsync(int id);

    // Lấy danh sách người dùng phân trang (có tìm kiếm).
    Task<PagedResult<UserResponseDto>> GetAllAsync(PaginationQuery query);

    // Tạo người dùng mới.
    Task<UserResponseDto> CreateAsync(CreateUserDto dto);

    // Cập nhật thông tin người dùng.
    Task<UserResponseDto> UpdateAsync(int id, UpdateUserDto dto);

    // Xóa người dùng theo Id.
    Task DeleteAsync(int id);
}
