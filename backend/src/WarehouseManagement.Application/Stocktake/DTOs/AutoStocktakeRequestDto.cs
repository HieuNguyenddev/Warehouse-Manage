// Lớp Application — DTO tạo phiếu kiểm kê tự động từ tồn hiện tại của kho.
using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Application.DTOs;

public class AutoStocktakeRequestDto
{
    // Id kho cần sinh phiếu kiểm kê
    [Range(1, int.MaxValue, ErrorMessage = "WarehouseId không hợp lệ.")]
    public int WarehouseId { get; set; }

    // Ghi chú phiếu
    [MaxLength(255, ErrorMessage = "Ghi chú tối đa 255 ký tự.")]
    public string? Note { get; set; }

    // true: số đếm = tồn hệ thống (chênh lệch 0); false: số đếm = 0 để nhập tay sau
    public bool MatchSystemQuantity { get; set; } = true;

    // Chỉ lấy các dòng tồn thấp (quantity <= minStock)
    public bool LowStockOnly { get; set; } = false;
}
