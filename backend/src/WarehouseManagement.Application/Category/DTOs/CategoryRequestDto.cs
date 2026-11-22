// Lớp Application — DTO tạo/cập nhật danh mục sản phẩm.
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.RegularExpressions;

namespace WarehouseManagement.Application.DTOs;

public class CategoryRequestDto : IValidatableObject
{
    public const int NameMinLength = 2;
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 255;

    private static readonly Regex NameRegex = new(
        @"^[\p{L}\p{M}0-9]+(?:[ _-][\p{L}\p{M}0-9]+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // Tên danh mục
    [Required(ErrorMessage = "Tên danh mục không được để trống.")]
    public string Name { get; set; } = string.Empty;

    // Mô tả danh mục (tùy chọn)
    public string? Description { get; set; }

    // Trạng thái kích hoạt
    public bool IsActive { get; set; } = true;


    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var error in ValidateName())
            yield return error;

        foreach (var error in ValidateDescription())
            yield return error;
    }

    private IEnumerable<ValidationResult> ValidateName()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return Error("Tên danh mục không được để trống hoặc chỉ chứa khoảng trắng.", nameof(Name));
            yield break;
        }

        if (Name != Name.Trim())
        {
            yield return Error("Tên danh mục không được có khoảng trắng ở đầu hoặc cuối.", nameof(Name));
            yield break;
        }

        if (Name.Length < NameMinLength || Name.Length > NameMaxLength)
        {
            yield return Error(
                $"Tên danh mục phải từ {NameMinLength} đến {NameMaxLength} ký tự.",
                nameof(Name));
            yield break;
        }

        if (Name.Contains("  ") || Name.Contains("__") || Name.Contains("--"))
        {
            yield return Error("Tên danh mục không được chứa khoảng trắng hoặc dấu phân cách liền nhau.", nameof(Name));
            yield break;
        }

        if (!Name.IsNormalized(NormalizationForm.FormC))
        {
            yield return Error("Tên danh mục không đúng định dạng Unicode.", nameof(Name));
            yield break;
        }

        if (!NameRegex.IsMatch(Name))
        {
            yield return Error(
                "Tên danh mục chỉ được chứa chữ, số, khoảng trắng, gạch dưới (_) hoặc gạch ngang (-).",
                nameof(Name));
            yield break;
        }

        if (Name.Any(char.IsControl))
        {
            yield return Error("Tên danh mục không được chứa ký tự điều khiển.", nameof(Name));
            yield break;
        }

        if (Name.Contains('<') || Name.Contains('>') || Name.Contains('&'))
        {
            yield return Error("Tên danh mục không được chứa ký tự HTML (&, <, >).", nameof(Name));
        }
    }

    private IEnumerable<ValidationResult> ValidateDescription()
    {
        if (Description is null)
            yield break;

        if (string.IsNullOrWhiteSpace(Description))
        {
            yield return Error("Mô tả không được chỉ chứa khoảng trắng.", nameof(Description));
            yield break;
        }

        if (Description != Description.Trim())
        {
            yield return Error("Mô tả không được có khoảng trắng ở đầu hoặc cuối.", nameof(Description));
            yield break;
        }

        if (Description.Length > DescriptionMaxLength)
        {
            yield return Error($"Mô tả không được vượt quá {DescriptionMaxLength} ký tự.", nameof(Description));
            yield break;
        }

        if (Description.Any(char.IsControl))
        {
            yield return Error("Mô tả không được chứa ký tự điều khiển.", nameof(Description));
            yield break;
        }

        if (Description.Contains('<') || Description.Contains('>') || Description.Contains('&'))
        {
            yield return Error("Mô tả không được chứa ký tự HTML (&, <, >).", nameof(Description));
        }
    }

    private static ValidationResult Error(string message, string field)
        => new(message, [field]);
}
