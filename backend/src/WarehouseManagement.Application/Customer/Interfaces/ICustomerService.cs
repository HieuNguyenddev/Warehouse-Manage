// Lớp Application — hợp đồng nghiệp vụ quản lý khách hàng (admin + tự phục vụ hồ sơ).
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Interfaces;

public interface ICustomerService
{
    // Lấy khách hàng theo Id.
    Task<CustomerResponseDto> GetByIdAsync(int id);

    // Lấy khách hàng gắn với UserId đăng nhập.
    Task<CustomerResponseDto> GetByUserIdAsync(int userId);

    // Lấy danh sách khách hàng phân trang.
    Task<PagedResult<CustomerResponseDto>> GetAllAsync(PaginationQuery query);

    // Tạo khách hàng mới (có thể kèm tài khoản User).
    Task<CustomerResponseDto> CreateAsync(CustomerRequestDto dto);

    // Cập nhật khách hàng (admin).
    Task<CustomerResponseDto> UpdateAsync(int id, CustomerRequestDto dto);

    // Xóa khách hàng theo Id.
    Task DeleteAsync(int id);

    // Upload avatar cho khách hàng theo Id (admin).
    Task<CustomerResponseDto> UploadAvatarAsync(
        int id,
        Stream fileStream,
        string fileName,
        string? contentType = null);

    // Khách hàng tự cập nhật hồ sơ theo UserId đăng nhập.
    Task<CustomerResponseDto> UpdateProfileByUserIdAsync(int userId, CustomerProfileUpdateDto dto);

    // Khách hàng tự upload avatar theo UserId đăng nhập.
    Task<CustomerResponseDto> UploadAvatarByUserIdAsync(
        int userId,
        Stream fileStream,
        string fileName,
        string? contentType = null);
}
