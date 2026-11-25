// Lớp Application — DTO tạo/cập nhật phiếu xuất kho (header + chi tiết).
using System.ComponentModel.DataAnnotations;
using WarehouseManagement.Domain;

namespace WarehouseManagement.Application.DTOs;

public class ExportReceiptRequestDto : IValidatableObject
{
    // Id kho xuất hàng
    [Range(1, int.MaxValue, ErrorMessage = "WarehouseId không hợp lệ.")]
    public int WarehouseId { get; set; }

    // Id khách hàng nhận hàng
    [Range(1, int.MaxValue, ErrorMessage = "CustomerId không hợp lệ.")]
    public int CustomerId { get; set; }

    // Ngày phiếu (null = lấy ngày hiện tại phía service)
    public DateTime? ReceiptDate { get; set; }

    // Nguồn phiếu: mặc định Manual; checkout store gửi Store
    public ExportReceiptSource Source { get; set; } = ExportReceiptSource.Manual;

    /// <summary>Mục đích xuất kho dược.</summary>
    public ExportIssuePurpose IssuePurpose { get; set; } = ExportIssuePurpose.DepartmentIssue;

    // Ghi chú phiếu
    [MaxLength(255, ErrorMessage = "Ghi chú tối đa 255 ký tự.")]
    public string? Note { get; set; }

    // Danh sách dòng chi tiết xuất
    [Required(ErrorMessage = "Phiếu xuất phải có ít nhất 1 dòng chi tiết.")]
    [MinLength(1, ErrorMessage = "Phiếu xuất phải có ít nhất 1 dòng chi tiết.")]
    public List<ExportReceiptDetailRequestDto> Details { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Details is null || Details.Count == 0)
        {
            yield return new ValidationResult("Phiếu xuất phải có ít nhất 1 dòng chi tiết.", [nameof(Details)]);
            yield break;
        }

        if (Details.Count > 100)
            yield return new ValidationResult("Mỗi phiếu tối đa 100 dòng chi tiết.", [nameof(Details)]);

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
