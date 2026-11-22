// Lớp Application — DTO cập nhật người dùng (mật khẩu tùy chọn).
using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Application.DTOs;

public class UpdateUserDto : IValidatableObject
{
    // Họ và tên hiển thị
    [Required(ErrorMessage = "Họ tên không được để trống.")]
    public string FullName { get; set; } = string.Empty;

    // Email liên hệ
    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự.")]
    public string Email { get; set; } = string.Empty;

    // Số điện thoại (tùy chọn)
    public string? Phone { get; set; }

    // Id vai trò gán cho user
    [Range(1, int.MaxValue, ErrorMessage = "RoleId không hợp lệ.")]
    public int RoleId { get; set; }

    // Trạng thái kích hoạt tài khoản
    public bool IsActive { get; set; } = true;

    // Mật khẩu mới; null = không đổi mật khẩu
    public string? Password { get; set; }


    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var e in CreateUserDto.ValidateFullName(FullName)) yield return e;
        foreach (var e in CreateUserDto.ValidatePhone(Phone)) yield return e;
        foreach (var e in CreateUserDto.ValidatePassword(Password, required: false)) yield return e;
    }
}
