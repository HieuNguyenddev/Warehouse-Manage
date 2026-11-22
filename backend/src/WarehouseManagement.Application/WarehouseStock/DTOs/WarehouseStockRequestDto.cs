// Lớp Application — DTO khởi tạo tồn đầu kỳ cho cặp kho + sản phẩm chưa có dòng tồn.
using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Application.DTOs;

public class WarehouseStockRequestDto
{
    // Id kho
    [Range(1, int.MaxValue, ErrorMessage = "WarehouseId không hợp lệ.")]
    public int WarehouseId { get; set; }

    // Id sản phẩm
    [Range(1, int.MaxValue, ErrorMessage = "ProductId không hợp lệ.")]
    public int ProductId { get; set; }

    // Số lượng tồn đầu kỳ
    [Range(0, double.MaxValue, ErrorMessage = "Số lượng tồn phải >= 0.")]
    public decimal Quantity { get; set; }

    // Ngưỡng min riêng theo kho (null = dùng Product.MinStock)
    [Range(0, int.MaxValue, ErrorMessage = "MinQuantity không hợp lệ.")]
    public int? MinQuantity { get; set; }

    // Lý do khởi tạo tồn (bắt buộc ghi sổ)
    [Required(ErrorMessage = "Lý do khởi tạo tồn là bắt buộc.")]
    [MinLength(3, ErrorMessage = "Lý do tối thiểu 3 ký tự.")]
    [MaxLength(255)]
    public string Reason { get; set; } = string.Empty;
}
