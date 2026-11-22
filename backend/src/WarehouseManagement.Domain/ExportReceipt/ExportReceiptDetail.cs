namespace WarehouseManagement.Domain;

// Chi tiết sản phẩm trên phiếu xuất kho.
// Mỗi dòng ghi số lượng, đơn giá và thành tiền của một sản phẩm xuất.
public class ExportReceiptDetail : BaseEntity
{
    public int ExportReceiptId { get; set; } // FK phiếu xuất
    public ExportReceipt? ExportReceipt { get; set; } // Navigation: phiếu xuất cha

    public int ProductId { get; set; } // FK sản phẩm xuất
    public Product? Product { get; set; } // Navigation: sản phẩm

    public decimal Quantity { get; set; } // Số lượng xuất
    public decimal UnitPrice { get; set; } // Đơn giá xuất
    public decimal LineTotal { get; set; } // Thành tiền dòng = Quantity * UnitPrice

    public string? Note { get; set; } // Ghi chú theo dòng
}
