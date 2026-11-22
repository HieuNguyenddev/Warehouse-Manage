// Lớp Application — DTO tạo/cập nhật khách hàng (có thể kèm tài khoản đăng nhập).
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.RegularExpressions;

namespace WarehouseManagement.Application.DTOs;

public class CustomerRequestDto : IValidatableObject
{
    private static readonly Regex NameRegex = new(
        @"^[\p{L}\p{M}0-9]+(?:[ &\-_./][\p{L}\p{M}0-9]+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex PhoneRegex = new(
        @"^(0|\+84)[0-9]{9,10}$",
        RegexOptions.Compiled);

    private static readonly Regex TaxCodeRegex = new(
        @"^[0-9]{10}(-[0-9]{3})?$",
        RegexOptions.Compiled);

    // Tên khách hàng / công ty
    [Required(ErrorMessage = "Tên khách hàng không được để trống.")]
    public string Name { get; set; } = string.Empty;

    // Người liên hệ
    public string? ContactName { get; set; }

    // Số điện thoại
    public string? Phone { get; set; }

    // Email
    public string? Email { get; set; }

    // Địa chỉ
    public string? Address { get; set; }

    // Mã số thuế
    public string? TaxCode { get; set; }

    // Ghi chú nội bộ
    public string? Note { get; set; }

    // Trạng thái kích hoạt
    public bool IsActive { get; set; } = true;

    // Username tạo tài khoản đăng nhập (role Customer); cùng có/không với Password
    public string? Username { get; set; }

    // Password tạo tài khoản đăng nhập; cùng có/không với Username
    public string? Password { get; set; }


    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var e in ValidateRequiredText(Name, nameof(Name), "Tên khách hàng", 2, 150, NameRegex,
                     "Tên chỉ được chứa chữ, số và các ký tự & - _ . /"))
            yield return e;

        foreach (var e in ValidateOptionalText(ContactName, nameof(ContactName), "Người liên hệ", 2, 100))
            yield return e;

        foreach (var e in ValidatePhone())
            yield return e;

        foreach (var e in ValidateEmail())
            yield return e;

        foreach (var e in ValidateOptionalText(Address, nameof(Address), "Địa chỉ", 5, 255))
            yield return e;

        foreach (var e in ValidateTaxCode())
            yield return e;

        foreach (var e in ValidateOptionalText(Note, nameof(Note), "Ghi chú", 1, 255))
            yield return e;

        var hasUser = !string.IsNullOrWhiteSpace(Username);
        var hasPass = !string.IsNullOrEmpty(Password);

        if (hasUser != hasPass)
        {
            yield return Err("Phải nhập đủ Username và Password để tạo tài khoản đăng nhập.", nameof(Username));
            yield break;
        }

        if (hasUser)
        {
            foreach (var e in CreateUserDto.ValidateUsername(Username))
                yield return e;

            foreach (var e in CreateUserDto.ValidatePassword(Password, required: true))
                yield return e;

            if (string.IsNullOrWhiteSpace(Email))
                yield return Err("Cần có Email khi tạo tài khoản đăng nhập cho khách hàng.", nameof(Email));
        }
    }

    private IEnumerable<ValidationResult> ValidatePhone()
    {
        if (Phone is null) yield break;

        if (string.IsNullOrWhiteSpace(Phone) || Phone != Phone.Trim())
        {
            yield return Err("Số điện thoại không hợp lệ.", nameof(Phone));
            yield break;
        }

        if (!PhoneRegex.IsMatch(Phone))
            yield return Err("Số điện thoại không đúng (ví dụ: 0912345678 hoặc +84912345678).", nameof(Phone));
    }

    private IEnumerable<ValidationResult> ValidateEmail()
    {
        if (Email is null) yield break;

        if (string.IsNullOrWhiteSpace(Email) || Email != Email.Trim())
        {
            yield return Err("Email không hợp lệ.", nameof(Email));
            yield break;
        }

        if (Email.Length > 100)
        {
            yield return Err("Email tối đa 100 ký tự.", nameof(Email));
            yield break;
        }

        if (!new EmailAddressAttribute().IsValid(Email))
            yield return Err("Email không đúng định dạng.", nameof(Email));
    }

    private IEnumerable<ValidationResult> ValidateTaxCode()
    {
        if (TaxCode is null) yield break;

        if (string.IsNullOrWhiteSpace(TaxCode) || TaxCode != TaxCode.Trim())
        {
            yield return Err("Mã số thuế không hợp lệ.", nameof(TaxCode));
            yield break;
        }

        if (!TaxCodeRegex.IsMatch(TaxCode))
            yield return Err("Mã số thuế phải gồm 10 số hoặc 10 số-3 số (ví dụ: 0123456789 hoặc 0123456789-001).", nameof(TaxCode));
    }

    private static IEnumerable<ValidationResult> ValidateRequiredText(
        string? value, string field, string label, int min, int max, Regex? regex = null, string? regexMsg = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            yield return Err($"{label} không được để trống hoặc chỉ chứa khoảng trắng.", field);
            yield break;
        }

        if (value != value.Trim())
        {
            yield return Err($"{label} không được có khoảng trắng đầu/cuối.", field);
            yield break;
        }

        if (value.Length < min || value.Length > max)
        {
            yield return Err($"{label} phải từ {min} đến {max} ký tự.", field);
            yield break;
        }

        if (!value.IsNormalized(NormalizationForm.FormC))
        {
            yield return Err($"{label} không đúng định dạng Unicode.", field);
            yield break;
        }

        if (regex is not null && !regex.IsMatch(value))
        {
            yield return Err(regexMsg ?? $"{label} chứa ký tự không hợp lệ.", field);
            yield break;
        }

        if (value.Any(char.IsControl) || value.Contains('<') || value.Contains('>'))
            yield return Err($"{label} chứa ký tự không hợp lệ.", field);
    }

    private static IEnumerable<ValidationResult> ValidateOptionalText(
        string? value, string field, string label, int min, int max)
    {
        if (value is null) yield break;

        foreach (var e in ValidateRequiredText(value, field, label, min, max))
            yield return e;
    }

    private static ValidationResult Err(string message, string field)
        => new(message, [field]);
}
