// Lớp Application — DTO cập nhật ngưỡng cảnh báo tồn theo kho (null = dùng Product.MinStock).
using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Application.DTOs;

public class WarehouseStockMinDto
{
    // Ngưỡng min riêng theo kho; null = dùng Product.MinStock
    [Range(0, int.MaxValue, ErrorMessage = "MinQuantity không hợp lệ.")]
    public int? MinQuantity { get; set; }
}
