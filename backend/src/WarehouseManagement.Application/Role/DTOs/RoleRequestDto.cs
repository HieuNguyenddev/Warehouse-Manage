// Lớp Application — DTO tạo/cập nhật vai trò (validate các case thường gặp khi test API).
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.RegularExpressions;

namespace WarehouseManagement.Application.DTOs;

public class RoleRequestDto : IValidatableObject
{
    public const int NameMinLength = 2;
    public const int NameMaxLength = 50;
    public const int DescriptionMaxLength = 255;

    // Chữ (mọi ngôn ngữ), số; giữa các từ cho phép space / _ / -
    // Ví dụ hợp lệ: Admin, Kho_Hang, Quan-Ly Kho
    private static readonly Regex NameRegex = new(
        @"^[\p{L}\p{M}0-9]+(?:[ _-][\p{L}\p{M}0-9]+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // Tên vai trò
    [Required(ErrorMessage = "Tên vai trò không được để trống.")]
    public string Name { get; set; } = string.Empty;

    // Mô tả vai trò (tùy chọn)
    public string? Description { get; set; }


    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var error in ValidateName())
            yield return error;

        foreach (var error in ValidateDescription())
            yield return error;
    }

    private IEnumerable<ValidationResult> ValidateName()
    {
        // Case 1: null / rỗng / chỉ khoảng trắng
        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return Error("Tên vai trò không được để trống hoặc chỉ chứa khoảng trắng.", nameof(Name));
            yield break;
        }

        // Case 2: khoảng trắng đầu/cuối
        if (Name != Name.Trim())
        {
            yield return Error("Tên vai trò không được có khoảng trắng ở đầu hoặc cuối.", nameof(Name));
            yield break;
        }

        // Case 3: độ dài 2..50 (khớp cột DB)
        if (Name.Length < NameMinLength || Name.Length > NameMaxLength)
        {
            yield return Error(
                $"Tên vai trò phải từ {NameMinLength} đến {NameMaxLength} ký tự.",
                nameof(Name));
            yield break;
        }

        // Case 4: khoảng trắng kép / ký tự phân cách liền nhau
        if (Name.Contains("  ") || Name.Contains("__") || Name.Contains("--")
            || Name.Contains(" _") || Name.Contains("_ ") || Name.Contains(" -") || Name.Contains("- ")
            || Name.Contains("_-") || Name.Contains("-_"))
        {
            yield return Error("Tên vai trò không được chứa khoảng trắng hoặc dấu phân cách liền nhau.", nameof(Name));
            yield break;
        }

        // Case 5: Unicode chuẩn FormC (tránh chữ Việt tổ hợp sai)
        if (!Name.IsNormalized(NormalizationForm.FormC))
        {
            yield return Error("Tên vai trò không đúng định dạng Unicode.", nameof(Name));
            yield break;
        }

        // Case 6: ký tự không hợp lệ (chỉ chữ, số, space, _, -)
        if (!NameRegex.IsMatch(Name))
        {
            yield return Error(
                "Tên vai trò chỉ được chứa chữ, số, khoảng trắng, gạch dưới (_) hoặc gạch ngang (-).",
                nameof(Name));
            yield break;
        }

        // Case 7: ký tự điều khiển (\n, \t, \0...)
        if (Name.Any(char.IsControl))
        {
            yield return Error("Tên vai trò không được chứa ký tự điều khiển.", nameof(Name));
            yield break;
        }

        // Case 8: chặn HTML / XSS đơn giản
        if (Name.Contains('<') || Name.Contains('>') || Name.Contains('&'))
        {
            yield return Error("Tên vai trò không được chứa ký tự HTML (&, <, >).", nameof(Name));
        }
    }

    private IEnumerable<ValidationResult> ValidateDescription()
    {
        // null = không gửi mô tả -> OK
        if (Description is null)
            yield break;

        // Case 9: mô tả chỉ toàn khoảng trắng
        if (string.IsNullOrWhiteSpace(Description))
        {
            yield return Error("Mô tả không được chỉ chứa khoảng trắng.", nameof(Description));
            yield break;
        }

        // Case 10: khoảng trắng đầu/cuối
        if (Description != Description.Trim())
        {
            yield return Error("Mô tả không được có khoảng trắng ở đầu hoặc cuối.", nameof(Description));
            yield break;
        }

        // Case 11: độ dài tối đa 255 (khớp cột DB)
        if (Description.Length > DescriptionMaxLength)
        {
            yield return Error($"Mô tả không được vượt quá {DescriptionMaxLength} ký tự.", nameof(Description));
            yield break;
        }

        // Case 12: ký tự điều khiển
        if (Description.Any(char.IsControl))
        {
            yield return Error("Mô tả không được chứa ký tự điều khiển.", nameof(Description));
            yield break;
        }

        // Case 13: chặn HTML / XSS đơn giản
        if (Description.Contains('<') || Description.Contains('>') || Description.Contains('&'))
        {
            yield return Error("Mô tả không được chứa ký tự HTML (&, <, >).", nameof(Description));
        }
    }

    private static ValidationResult Error(string message, string field)
        => new(message, [field]);
}
