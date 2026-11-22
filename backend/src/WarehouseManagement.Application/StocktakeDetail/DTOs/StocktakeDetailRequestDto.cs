// Lớp Application — DTO thêm dòng sản phẩm vào phiếu kiểm kê.
using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Application.DTOs;

public class StocktakeDetailRequestDto
{
    // Id sản phẩm kiểm kê
    [Range(1, int.MaxValue, ErrorMessage = "ProductId không hợp lệ.")]
    public int ProductId { get; set; }

    // Số lượng thực tế đếm được
    [Range(0, double.MaxValue, ErrorMessage = "Số lượng đếm phải >= 0.")]
    public decimal CountedQuantity { get; set; }

    // Ghi chú dòng
    [MaxLength(255, ErrorMessage = "Ghi chú dòng tối đa 255 ký tự.")]
    public string? Note { get; set; }
}
