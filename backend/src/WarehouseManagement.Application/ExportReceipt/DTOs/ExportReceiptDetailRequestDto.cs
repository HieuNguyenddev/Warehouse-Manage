// Lớp Application — DTO một dòng chi tiết trên phiếu xuất (request).
using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Application.DTOs;

public class ExportReceiptDetailRequestDto
{
    // Id sản phẩm xuất
    [Range(1, int.MaxValue, ErrorMessage = "ProductId không hợp lệ.")]
    public int ProductId { get; set; }

    // Số lượng xuất (> 0)
    [Range(0.01, double.MaxValue, ErrorMessage = "Số lượng xuất phải > 0.")]
    public decimal Quantity { get; set; }

    // Đơn giá xuất
    [Range(0, double.MaxValue, ErrorMessage = "Đơn giá phải >= 0.")]
    public decimal UnitPrice { get; set; }

    // Ghi chú dòng
    [MaxLength(255, ErrorMessage = "Ghi chú dòng tối đa 255 ký tự.")]
    public string? Note { get; set; }
}
