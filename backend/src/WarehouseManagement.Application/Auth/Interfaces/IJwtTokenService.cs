// Lớp Application — hợp đồng tạo JWT access token cho người dùng.
using WarehouseManagement.Domain;

namespace WarehouseManagement.Application.Interfaces;

public interface IJwtTokenService
{
    // Sinh access token JWT từ thông tin User; trả thêm thời điểm hết hạn qua out.
    string GenerateAccessToken(User user, out DateTime expiresAt);
}
