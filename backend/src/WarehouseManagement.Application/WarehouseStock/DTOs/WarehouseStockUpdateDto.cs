// Lớp Application — DTO cập nhật tồn cũ (deprecated: dùng WarehouseStockAdjustDto + POST .../adjust).
using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Application.DTOs;

public class WarehouseStockUpdateDto
{
    // Số lượng tồn mới
    [Range(0, double.MaxValue, ErrorMessage = "Số lượng tồn phải >= 0.")]
    public decimal Quantity { get; set; }

    // Lý do (tùy chọn trên API cũ)
    [MaxLength(255)]
    public string? Reason { get; set; }
}
