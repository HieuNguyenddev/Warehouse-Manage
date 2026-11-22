// Lớp Application — DTO phản hồi thông tin người dùng (không gồm mật khẩu).
namespace WarehouseManagement.Application.DTOs;

public class UserResponseDto
{
    // Khóa chính người dùng
    public int Id { get; set; }

    // Tên đăng nhập
    public string Username { get; set; } = string.Empty;

    // Họ và tên hiển thị
    public string FullName { get; set; } = string.Empty;

    // Email liên hệ
    public string Email { get; set; } = string.Empty;

    // Số điện thoại (có thể null)
    public string? Phone { get; set; }

    // Trạng thái kích hoạt tài khoản
    public bool IsActive { get; set; }

    // Id vai trò gán cho user
    public int RoleId { get; set; }

    // Tên vai trò (denormalize để hiển thị)
    public string? RoleName { get; set; }

    // Thời điểm tạo
    public DateTime CreatedAt { get; set; }

    // Thời điểm cập nhật gần nhất
    public DateTime? UpdatedAt { get; set; }
}
