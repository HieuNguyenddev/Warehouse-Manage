// Lớp Application — DTO khách hàng tự cập nhật hồ sơ (cổng Store).
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace WarehouseManagement.Application.DTOs;

public class CustomerProfileUpdateDto : IValidatableObject
{
    private static readonly Regex PhoneRegex = new(
        @"^(0|\+84)[0-9]{9,10}$",
        RegexOptions.Compiled);

    // Tên hiển thị / tên công ty
    [Required(ErrorMessage = "Tên không được để trống.")]
    [MinLength(2), MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    // Người liên hệ
    [MaxLength(100)]
    public string? ContactName { get; set; }

    // Số điện thoại
    [MaxLength(20)]
    public string? Phone { get; set; }

    // Email
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [MaxLength(100)]
    public string? Email { get; set; }

    // Địa chỉ
    [MaxLength(255)]
    public string? Address { get; set; }

    // Ghi chú
    [MaxLength(255)]
    public string? Note { get; set; }

    // Mật khẩu mới; để trống nếu không đổi
    [MaxLength(100)]
    public string? NewPassword { get; set; }


    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name) || Name != Name.Trim())
            yield return new ValidationResult("Tên không hợp lệ.", [nameof(Name)]);

        if (!string.IsNullOrWhiteSpace(Phone) && !PhoneRegex.IsMatch(Phone.Trim()))
            yield return new ValidationResult(
                "Số điện thoại không đúng (ví dụ: 0912345678).",
                [nameof(Phone)]);

        if (!string.IsNullOrEmpty(NewPassword))
        {
            foreach (var e in CreateUserDto.ValidatePassword(NewPassword, required: false))
                yield return e;
        }
    }
}
