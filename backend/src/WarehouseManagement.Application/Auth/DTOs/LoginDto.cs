// Lớp Application — DTO đầu vào đăng nhập (username + mật khẩu).
using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Application.DTOs;

public class LoginDto
{
    // Tên đăng nhập
    [Required(ErrorMessage = "Username không được để trống.")]
    public string Username { get; set; } = string.Empty;

    // Mật khẩu
    [Required(ErrorMessage = "Mật khẩu không được để trống.")]
    public string Password { get; set; } = string.Empty;
}
