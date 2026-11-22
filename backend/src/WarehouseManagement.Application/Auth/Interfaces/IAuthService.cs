// Lớp Application — hợp đồng dịch vụ xác thực (đăng nhập theo portal).
using WarehouseManagement.Application.DTOs;

namespace WarehouseManagement.Application.Interfaces;

public interface IAuthService
{
    // Đăng nhập theo portal (Admin hoặc Customer), trả về token và thông tin user.
    Task<LoginResponseDto> LoginAsync(LoginDto dto, string portal);
}
