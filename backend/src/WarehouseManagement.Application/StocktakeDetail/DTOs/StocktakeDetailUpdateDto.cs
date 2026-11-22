// Lớp Application — DTO cập nhật số đếm / ghi chú của 1 dòng kiểm kê (chỉ Draft).
using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Application.DTOs;

public class StocktakeDetailUpdateDto
{
    // Số lượng thực tế đếm được (mới)
    [Range(0, double.MaxValue, ErrorMessage = "Số lượng đếm phải >= 0.")]
    public decimal CountedQuantity { get; set; }

    // Ghi chú dòng
    [MaxLength(255, ErrorMessage = "Ghi chú dòng tối đa 255 ký tự.")]
    public string? Note { get; set; }
}
