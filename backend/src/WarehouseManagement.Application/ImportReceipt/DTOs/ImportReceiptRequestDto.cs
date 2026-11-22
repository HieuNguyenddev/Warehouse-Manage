// Lớp Application — DTO tạo/cập nhật phiếu nhập kho (header + chi tiết).
using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Application.DTOs;

public class ImportReceiptRequestDto : IValidatableObject
{
    // Id kho nhận hàng
    [Range(1, int.MaxValue, ErrorMessage = "WarehouseId không hợp lệ.")]
    public int WarehouseId { get; set; }

    // Id nhà cung cấp
    [Range(1, int.MaxValue, ErrorMessage = "SupplierId không hợp lệ.")]
    public int SupplierId { get; set; }

    // Ngày phiếu (null = lấy ngày hiện tại phía service)
    public DateTime? ReceiptDate { get; set; }

    // Ghi chú phiếu
    [MaxLength(255, ErrorMessage = "Ghi chú tối đa 255 ký tự.")]
    public string? Note { get; set; }

    // Danh sách dòng chi tiết nhập
    [Required(ErrorMessage = "Phiếu nhập phải có ít nhất 1 dòng chi tiết.")]
    [MinLength(1, ErrorMessage = "Phiếu nhập phải có ít nhất 1 dòng chi tiết.")]
    public List<ImportReceiptDetailRequestDto> Details { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Details is null || Details.Count == 0)
        {
            yield return new ValidationResult("Phiếu nhập phải có ít nhất 1 dòng chi tiết.", [nameof(Details)]);
            yield break;
        }

        if (Details.Count > 100)
            yield return new ValidationResult("Mỗi phiếu tối đa 100 dòng chi tiết.", [nameof(Details)]);

        // Không cho trùng ProductId trong cùng phiếu
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
