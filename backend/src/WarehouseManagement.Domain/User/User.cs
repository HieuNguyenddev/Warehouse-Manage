namespace WarehouseManagement.Domain;

/// <summary>
/// USER — tài khoản đăng nhập hệ thống.
///
/// Lưu ý bảo mật cho sinh viên:
/// - KHÔNG lưu mật khẩu thô. Chỉ lưu PasswordHash.
/// - Role quyết định vào được cổng Admin hay Store.
/// </summary>
public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;

    // FK → Role
    public int RoleId { get; set; }
    public Role? Role { get; set; }

    // Nếu là khách hàng: có hồ sơ Customer (1-1, tùy chọn)
    public Customer? Customer { get; set; }
}
