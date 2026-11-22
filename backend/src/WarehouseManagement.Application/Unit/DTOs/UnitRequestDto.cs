// Lớp Application — DTO tạo/cập nhật đơn vị tính.
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.RegularExpressions;

namespace WarehouseManagement.Application.DTOs;

public class UnitRequestDto : IValidatableObject
{
    private static readonly Regex NameRegex = new(
        @"^[\p{L}\p{M}0-9]+(?:[ _-][\p{L}\p{M}0-9]+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex SymbolRegex = new(
        @"^[\p{L}\p{M}0-9.%]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // Tên đơn vị tính
    [Required(ErrorMessage = "Tên đơn vị không được để trống.")]
    public string Name { get; set; } = string.Empty;

    // Ký hiệu ngắn của đơn vị
    [Required(ErrorMessage = "Ký hiệu đơn vị không được để trống.")]
    public string Symbol { get; set; } = string.Empty;

    // Mô tả đơn vị (tùy chọn)
    public string? Description { get; set; }

    // Trạng thái kích hoạt
    public bool IsActive { get; set; } = true;


    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var e in ValidateName()) yield return e;
        foreach (var e in ValidateSymbol()) yield return e;
        foreach (var e in ValidateDescription()) yield return e;
    }

    private IEnumerable<ValidationResult> ValidateName()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return Err("Tên đơn vị không được để trống hoặc chỉ chứa khoảng trắng.", nameof(Name));
            yield break;
        }

        if (Name != Name.Trim())
        {
            yield return Err("Tên đơn vị không được có khoảng trắng đầu/cuối.", nameof(Name));
            yield break;
        }

        if (Name.Length < 1 || Name.Length > 50)
        {
            yield return Err("Tên đơn vị phải từ 1 đến 50 ký tự.", nameof(Name));
            yield break;
        }

        if (!Name.IsNormalized(NormalizationForm.FormC) || !NameRegex.IsMatch(Name))
        {
            yield return Err("Tên đơn vị chỉ được chứa chữ, số, khoảng trắng, _ hoặc -.", nameof(Name));
            yield break;
        }

        if (Name.Any(char.IsControl) || Name.Contains('<') || Name.Contains('>') || Name.Contains('&'))
        {
            yield return Err("Tên đơn vị chứa ký tự không hợp lệ.", nameof(Name));
        }
    }

    private IEnumerable<ValidationResult> ValidateSymbol()
    {
        if (string.IsNullOrWhiteSpace(Symbol))
        {
            yield return Err("Ký hiệu đơn vị không được để trống hoặc chỉ chứa khoảng trắng.", nameof(Symbol));
            yield break;
        }

        if (Symbol != Symbol.Trim())
        {
            yield return Err("Ký hiệu không được có khoảng trắng đầu/cuối.", nameof(Symbol));
            yield break;
        }

        if (Symbol.Length < 1 || Symbol.Length > 20)
        {
            yield return Err("Ký hiệu phải từ 1 đến 20 ký tự.", nameof(Symbol));
            yield break;
        }

        if (Symbol.Contains(' '))
        {
            yield return Err("Ký hiệu không được chứa khoảng trắng.", nameof(Symbol));
            yield break;
        }

        if (!Symbol.IsNormalized(NormalizationForm.FormC) || !SymbolRegex.IsMatch(Symbol))
        {
            yield return Err("Ký hiệu chỉ được chứa chữ, số, dấu chấm (.) hoặc %.", nameof(Symbol));
            yield break;
        }

        if (Symbol.Any(char.IsControl) || Symbol.Contains('<') || Symbol.Contains('>') || Symbol.Contains('&'))
        {
            yield return Err("Ký hiệu chứa ký tự không hợp lệ.", nameof(Symbol));
        }
    }

    private IEnumerable<ValidationResult> ValidateDescription()
    {
        if (Description is null)
            yield break;

        if (string.IsNullOrWhiteSpace(Description))
        {
            yield return Err("Mô tả không được chỉ chứa khoảng trắng.", nameof(Description));
            yield break;
        }

        if (Description != Description.Trim())
        {
            yield return Err("Mô tả không được có khoảng trắng đầu/cuối.", nameof(Description));
            yield break;
        }

        if (Description.Length > 255)
        {
            yield return Err("Mô tả tối đa 255 ký tự.", nameof(Description));
            yield break;
        }

        if (Description.Any(char.IsControl) || Description.Contains('<') || Description.Contains('>') || Description.Contains('&'))
        {
            yield return Err("Mô tả chứa ký tự không hợp lệ.", nameof(Description));
        }
    }

    private static ValidationResult Err(string message, string field)
        => new(message, [field]);
}
