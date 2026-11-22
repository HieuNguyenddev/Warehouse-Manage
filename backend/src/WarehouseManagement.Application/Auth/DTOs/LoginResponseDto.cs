// Lớp Application — DTO phản hồi sau đăng nhập thành công (token + user + portal).
namespace WarehouseManagement.Application.DTOs;

public class LoginResponseDto
{
    // JWT access token
    public string Token { get; set; } = string.Empty;

    // Thời điểm token hết hạn (UTC/local tùy cấu hình JWT)
    public DateTime ExpiresAt { get; set; }

    // Cổng đăng nhập: "admin" | "customer" — frontend điều hướng theo cổng
    public string Portal { get; set; } = string.Empty;

    // Thông tin người dùng đã đăng nhập
    public UserResponseDto User { get; set; } = null!;

    // Id khách hàng nếu đăng nhập cổng Customer; null với Admin
    public int? CustomerId { get; set; }
}
