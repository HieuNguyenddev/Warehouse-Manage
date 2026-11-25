// Lớp Application — DTO một dòng chi tiết trên phiếu nhập (request).
using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Application.DTOs;

public class ImportReceiptDetailRequestDto
{
    // Id sản phẩm nhập
    [Range(1, int.MaxValue, ErrorMessage = "ProductId không hợp lệ.")]
    public int ProductId { get; set; }

    // Số lượng nhập (> 0)
    [Range(0.01, double.MaxValue, ErrorMessage = "Số lượng nhập phải > 0.")]
    public decimal Quantity { get; set; }

    // Giá nhập (mua từ NCC)
    [Range(0, double.MaxValue, ErrorMessage = "Giá nhập phải >= 0.")]
    public decimal UnitPrice { get; set; }

    /// <summary>Giá bán (đẩy lên Product.Price khi Confirm — trang người dùng).</summary>
    [Range(0, double.MaxValue, ErrorMessage = "Giá bán phải >= 0.")]
    public decimal SalePrice { get; set; }

    /// <summary>Số lô (khuyến nghị bắt buộc với thuốc có HSD).</summary>
    [MaxLength(50, ErrorMessage = "Số lô tối đa 50 ký tự.")]
    public string? LotNumber { get; set; }

    /// <summary>Hạn dùng của lô.</summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>Ngày sản xuất.</summary>
    public DateTime? ManufactureDate { get; set; }

    // Ghi chú dòng
    [MaxLength(255, ErrorMessage = "Ghi chú dòng tối đa 255 ký tự.")]
    public string? Note { get; set; }
}
