// Lớp Application — DTO tạo người dùng mới (kèm validation chi tiết).
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace WarehouseManagement.Application.DTOs;

public class CreateUserDto : IValidatableObject
{
    private static readonly Regex UsernameRegex = new(
        @"^[a-zA-Z0-9._-]{3,50}$",
        RegexOptions.Compiled);

    private static readonly Regex PhoneRegex = new(
        @"^(0|\+84)[0-9]{9,10}$",
        RegexOptions.Compiled);

    // Tên đăng nhập
    [Required(ErrorMessage = "Username không được để trống.")]
    public string Username { get; set; } = string.Empty;

    // Mật khẩu ban đầu
    [Required(ErrorMessage = "Mật khẩu không được để trống.")]
    public string Password { get; set; } = string.Empty;

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

    // Tài khoản có được kích hoạt ngay không
    public bool IsActive { get; set; } = true;


    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var e in ValidateUsername(Username)) yield return e;
        foreach (var e in ValidatePassword(Password, required: true)) yield return e;
        foreach (var e in ValidateFullName(FullName)) yield return e;
        foreach (var e in ValidatePhone(Phone)) yield return e;
    }

    internal static IEnumerable<ValidationResult> ValidateUsername(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            yield return Err("Username không được để trống hoặc chỉ chứa khoảng trắng.", nameof(Username));
            yield break;
        }

        if (username != username.Trim())
        {
            yield return Err("Username không được có khoảng trắng đầu/cuối.", nameof(Username));
            yield break;
        }

        if (!UsernameRegex.IsMatch(username))
        {
            yield return Err(
                "Username 3-50 ký tự, chỉ gồm chữ không dấu, số, dấu chấm (.), gạch dưới (_), gạch ngang (-).",
                nameof(Username));
        }
    }

    internal static IEnumerable<ValidationResult> ValidatePassword(string? password, bool required)
    {
        if (string.IsNullOrEmpty(password))
        {
            if (required)
                yield return Err("Mật khẩu không được để trống.", nameof(Password));
            yield break;
        }

        if (password.Length < 6 || password.Length > 100)
        {
            yield return Err("Mật khẩu phải từ 6 đến 100 ký tự.", nameof(Password));
            yield break;
        }

        if (password.Any(char.IsWhiteSpace))
        {
            yield return Err("Mật khẩu không được chứa khoảng trắng.", nameof(Password));
        }
    }

    internal static IEnumerable<ValidationResult> ValidateFullName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            yield return Err("Họ tên không được để trống hoặc chỉ chứa khoảng trắng.", nameof(FullName));
            yield break;
        }

        if (fullName != fullName.Trim())
        {
            yield return Err("Họ tên không được có khoảng trắng đầu/cuối.", nameof(FullName));
            yield break;
        }

        if (fullName.Length < 2 || fullName.Length > 100)
        {
            yield return Err("Họ tên phải từ 2 đến 100 ký tự.", nameof(FullName));
            yield break;
        }

        if (fullName.Contains('<') || fullName.Contains('>') || fullName.Contains('&'))
        {
            yield return Err("Họ tên không được chứa ký tự HTML.", nameof(FullName));
        }
    }

    internal static IEnumerable<ValidationResult> ValidatePhone(string? phone)
    {
        if (phone is null)
            yield break;

        if (string.IsNullOrWhiteSpace(phone))
        {
            yield return Err("Số điện thoại không được chỉ chứa khoảng trắng.", nameof(Phone));
            yield break;
        }

        if (phone != phone.Trim())
        {
            yield return Err("Số điện thoại không được có khoảng trắng đầu/cuối.", nameof(Phone));
            yield break;
        }

        if (!PhoneRegex.IsMatch(phone))
        {
            yield return Err("Số điện thoại không hợp lệ (ví dụ: 0912345678 hoặc +84912345678).", nameof(Phone));
        }
    }

    private static ValidationResult Err(string message, string field)
        => new(message, [field]);
}
