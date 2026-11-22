// Lớp Application — DTO tổng quan tồn kho (dashboard / thống kê nhanh).
namespace WarehouseManagement.Application.DTOs;

public class WarehouseStockSummaryDto
{
    // Tổng số SKU (dòng tồn) trong phạm vi lọc
    public int TotalSkus { get; set; }

    // Số dòng đang tồn thấp
    public int LowStockCount { get; set; }

    // Số dòng hết hàng
    public int OutOfStockCount { get; set; }

    // Tổng số lượng tồn
    public decimal TotalQuantity { get; set; }

    // Id kho đang lọc; null = toàn hệ thống
    public int? WarehouseId { get; set; }
}
