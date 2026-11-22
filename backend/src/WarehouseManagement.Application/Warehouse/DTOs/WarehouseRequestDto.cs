// Lớp Application — DTO tạo/cập nhật kho hàng.
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.RegularExpressions;

namespace WarehouseManagement.Application.DTOs;

public class WarehouseRequestDto : IValidatableObject
{
    private static readonly Regex CodeRegex = new(
        @"^[A-Za-z0-9][A-Za-z0-9\-_]{1,29}$",
        RegexOptions.Compiled);

    private static readonly Regex NameRegex = new(
        @"^[\p{L}\p{M}0-9]+(?:[ &\-_./][\p{L}\p{M}0-9]+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex PhoneRegex = new(
        @"^(0|\+84)[0-9]{9,10}$",
        RegexOptions.Compiled);

    // Mã kho (duy nhất)
    [Required(ErrorMessage = "Mã kho không được để trống.")]
    public string Code { get; set; } = string.Empty;

    // Tên kho
    [Required(ErrorMessage = "Tên kho không được để trống.")]
    public string Name { get; set; } = string.Empty;

    // Địa chỉ kho
    public string? Address { get; set; }

    // Số điện thoại kho
    public string? Phone { get; set; }

    // Tên người quản lý kho
    public string? ManagerName { get; set; }

    // Ghi chú
    public string? Note { get; set; }

    // Trạng thái kích hoạt
    public bool IsActive { get; set; } = true;


    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Code) || Code != Code.Trim())
        {
            yield return Err("Mã kho không hợp lệ.", nameof(Code));
            yield break;
        }

        if (!CodeRegex.IsMatch(Code))
            yield return Err("Mã kho 2-30 ký tự: chữ, số, gạch ngang, gạch dưới.", nameof(Code));

        if (string.IsNullOrWhiteSpace(Name) || Name != Name.Trim())
        {
            yield return Err("Tên kho không hợp lệ.", nameof(Name));
            yield break;
        }

        if (Name.Length < 2 || Name.Length > 150)
            yield return Err("Tên kho phải từ 2 đến 150 ký tự.", nameof(Name));
        else if (!Name.IsNormalized(NormalizationForm.FormC) || !NameRegex.IsMatch(Name))
            yield return Err("Tên kho chứa ký tự không hợp lệ.", nameof(Name));

        foreach (var e in ValidateOptionalText(Address, nameof(Address), "Địa chỉ", 5, 255))
            yield return e;

        foreach (var e in ValidatePhone())
            yield return e;

        foreach (var e in ValidateOptionalText(ManagerName, nameof(ManagerName), "Người quản lý", 2, 100))
            yield return e;

        foreach (var e in ValidateOptionalText(Note, nameof(Note), "Ghi chú", 1, 255))
            yield return e;
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

    private static IEnumerable<ValidationResult> ValidateOptionalText(
        string? value, string field, string label, int min, int max)
    {
        if (value is null) yield break;

        if (string.IsNullOrWhiteSpace(value) || value != value.Trim())
        {
            yield return Err($"{label} không hợp lệ.", field);
            yield break;
        }

        if (value.Length < min || value.Length > max)
            yield return Err($"{label} phải từ {min} đến {max} ký tự.", field);
        else if (value.Any(char.IsControl) || value.Contains('<') || value.Contains('>'))
            yield return Err($"{label} chứa ký tự không hợp lệ.", field);
    }

    private static ValidationResult Err(string message, string field)
        => new(message, [field]);
}
