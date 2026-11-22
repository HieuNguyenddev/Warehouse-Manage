// Lớp Application — DTO điều chỉnh tồn tuyệt đối (ghi sổ Manual + lý do).
using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Application.DTOs;

public class WarehouseStockAdjustDto
{
    // Số lượng tồn mới (tuyệt đối, không phải delta)
    [Range(0, double.MaxValue, ErrorMessage = "Số lượng tồn phải >= 0.")]
    public decimal Quantity { get; set; }

    // Lý do điều chỉnh (bắt buộc)
    [Required(ErrorMessage = "Lý do điều chỉnh là bắt buộc.")]
    [MinLength(3, ErrorMessage = "Lý do tối thiểu 3 ký tự.")]
    [MaxLength(255)]
    public string Reason { get; set; } = string.Empty;
}
