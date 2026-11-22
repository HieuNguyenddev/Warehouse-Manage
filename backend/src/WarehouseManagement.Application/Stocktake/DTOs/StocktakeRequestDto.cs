// Lớp Application — DTO tạo/cập nhật phiếu kiểm kê thủ công.
using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Application.DTOs;

public class StocktakeRequestDto : IValidatableObject
{
    // Id kho kiểm kê
    [Range(1, int.MaxValue, ErrorMessage = "WarehouseId không hợp lệ.")]
    public int WarehouseId { get; set; }

    // Ngày kiểm kê (null = ngày hiện tại phía service)
    public DateTime? StocktakeDate { get; set; }

    // Ghi chú phiếu
    [MaxLength(255, ErrorMessage = "Ghi chú tối đa 255 ký tự.")]
    public string? Note { get; set; }

    // Danh sách dòng sản phẩm kiểm kê
    [Required(ErrorMessage = "Phiếu kiểm kê phải có ít nhất 1 dòng chi tiết.")]
    [MinLength(1, ErrorMessage = "Phiếu kiểm kê phải có ít nhất 1 dòng chi tiết.")]
    public List<StocktakeDetailRequestDto> Details { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Details is null || Details.Count == 0)
        {
            yield return new ValidationResult("Phiếu kiểm kê phải có ít nhất 1 dòng chi tiết.", [nameof(Details)]);
            yield break;
        }

        if (Details.Count > 200)
            yield return new ValidationResult("Mỗi phiếu tối đa 200 dòng chi tiết.", [nameof(Details)]);

        var duplicates = Details
            .GroupBy(x => x.ProductId)
            .Where(g => g.Key > 0 && g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count > 0)
            yield return new ValidationResult(
                $"Sản phẩm bị trùng trên phiếu: {string.Join(", ", duplicates)}.",
                [nameof(Details)]);

        if (Note is not null && (string.IsNullOrWhiteSpace(Note) || Note != Note.Trim()))
            yield return new ValidationResult("Ghi chú không hợp lệ.", [nameof(Note)]);
    }
}
