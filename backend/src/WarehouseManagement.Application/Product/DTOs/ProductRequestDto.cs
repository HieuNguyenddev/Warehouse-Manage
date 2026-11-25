// Lớp Application — DTO tạo/cập nhật sản phẩm.
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.RegularExpressions;

namespace WarehouseManagement.Application.DTOs;

public class ProductRequestDto : IValidatableObject
{
    private static readonly Regex SkuRegex = new(
        @"^[A-Za-z0-9][A-Za-z0-9\-_]{1,49}$",
        RegexOptions.Compiled);

    private static readonly Regex NameRegex = new(
        @"^[\p{L}\p{M}0-9]+(?:[ _\-/][\p{L}\p{M}0-9]+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // Mã SKU sản phẩm (duy nhất)
    [Required(ErrorMessage = "SKU không được để trống.")]
    public string Sku { get; set; } = string.Empty;

    // Tên sản phẩm
    [Required(ErrorMessage = "Tên sản phẩm không được để trống.")]
    public string Name { get; set; } = string.Empty;

    // Mô tả sản phẩm
    public string? Description { get; set; }

    // Giá bán
    [Range(0, double.MaxValue, ErrorMessage = "Giá phải >= 0.")]
    public decimal Price { get; set; }

    // Ngưỡng tồn tối thiểu mặc định của sản phẩm
    [Range(0, int.MaxValue, ErrorMessage = "Tồn tối thiểu phải >= 0.")]
    public int MinStock { get; set; }

    // Id danh mục
    [Range(1, int.MaxValue, ErrorMessage = "CategoryId không hợp lệ.")]
    public int CategoryId { get; set; }

    // Id đơn vị tính
    [Range(1, int.MaxValue, ErrorMessage = "UnitId không hợp lệ.")]
    public int UnitId { get; set; }

    // Trạng thái kích hoạt
    public bool IsActive { get; set; } = true;

    // --- Thuộc tính dược BV ---
    [MaxLength(50)]
    public string? RegistrationNumber { get; set; }

    [MaxLength(200)]
    public string? ActiveIngredient { get; set; }

    [MaxLength(100)]
    public string? Strength { get; set; }

    [MaxLength(100)]
    public string? DosageForm { get; set; }

    [MaxLength(200)]
    public string? Manufacturer { get; set; }

    public bool IsControlledSubstance { get; set; }

    public bool RequiresColdStorage { get; set; }

    [Range(1, 730, ErrorMessage = "Số ngày cảnh báo HSD phải từ 1 đến 730.")]
    public int ExpiryAlertDays { get; set; } = 90;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Sku) || Sku != Sku.Trim())
        {
            yield return Err("SKU không hợp lệ.", nameof(Sku));
            yield break;
        }

        if (!SkuRegex.IsMatch(Sku))
            yield return Err("SKU 2-50 ký tự: chữ, số, gạch ngang, gạch dưới.", nameof(Sku));

        if (string.IsNullOrWhiteSpace(Name) || Name != Name.Trim())
        {
            yield return Err("Tên sản phẩm không hợp lệ.", nameof(Name));
            yield break;
        }

        if (Name.Length < 2 || Name.Length > 150)
            yield return Err("Tên sản phẩm phải từ 2 đến 150 ký tự.", nameof(Name));
        else if (!Name.IsNormalized(NormalizationForm.FormC) || !NameRegex.IsMatch(Name))
            yield return Err("Tên sản phẩm chứa ký tự không hợp lệ.", nameof(Name));

        if (Description is not null)
        {
            if (string.IsNullOrWhiteSpace(Description) || Description != Description.Trim())
                yield return Err("Mô tả không hợp lệ.", nameof(Description));
            else if (Description.Length > 1000)
                yield return Err("Mô tả tối đa 1000 ký tự.", nameof(Description));
        }
    }

    private static ValidationResult Err(string message, string field)
        => new(message, [field]);
}
