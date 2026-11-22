namespace WarehouseManagement.Domain;

// Tồn kho theo cặp sản phẩm + kho (unique ProductId + WarehouseId).
// Số lượng chỉ đổi qua phiếu nhập/xuất/kiểm kê hoặc điều chỉnh có lý do.
public class WarehouseStock : BaseEntity
{
    public int WarehouseId { get; set; } // FK kho
    public Warehouse? Warehouse { get; set; } // Navigation: kho chứa tồn

    public int ProductId { get; set; } // FK sản phẩm
    public Product? Product { get; set; } // Navigation: sản phẩm tồn kho

    // Tồn hiện có (on-hand)
    public decimal Quantity { get; set; } // Số lượng thực tế đang có trong kho

    // Ngưỡng cảnh báo riêng theo kho; null = dùng Product.MinStock
    public int? MinQuantity { get; set; } // Ngưỡng tồn tối thiểu riêng cho cặp kho-SP
}
