// Lớp Application — DTO phản hồi một dòng tồn kho theo kho–sản phẩm.
namespace WarehouseManagement.Application.DTOs;

public class WarehouseStockResponseDto
{
    // Khóa chính dòng tồn
    public int Id { get; set; }

    // Id kho
    public int WarehouseId { get; set; }

    // Mã kho (hiển thị)
    public string? WarehouseCode { get; set; }

    // Tên kho (hiển thị)
    public string? WarehouseName { get; set; }

    // Id sản phẩm
    public int ProductId { get; set; }

    // SKU sản phẩm
    public string? ProductSku { get; set; }

    // Tên sản phẩm
    public string? ProductName { get; set; }

    // Tên danh mục sản phẩm
    public string? CategoryName { get; set; }

    // Ký hiệu đơn vị tính
    public string? UnitSymbol { get; set; }

    // Min theo Product (mặc định toàn hệ thống)
    public int ProductMinStock { get; set; }

    // Min riêng theo kho (null = dùng ProductMinStock)
    public int? MinQuantity { get; set; }

    // Ngưỡng hiệu lực dùng để cảnh báo tồn thấp
    public int EffectiveMinStock { get; set; }

    // Số lượng tồn hiện tại
    public decimal Quantity { get; set; }

    // true khi EffectiveMinStock > 0 và Quantity <= EffectiveMinStock
    public bool IsLowStock { get; set; }

    // true khi Quantity <= 0
    public bool IsOutOfStock { get; set; }

    // Thời điểm tạo dòng tồn
    public DateTime CreatedAt { get; set; }

    // Thời điểm cập nhật gần nhất
    public DateTime? UpdatedAt { get; set; }

    // Alias tương thích frontend cũ (= EffectiveMinStock)
    public int MinStock => EffectiveMinStock;
}
