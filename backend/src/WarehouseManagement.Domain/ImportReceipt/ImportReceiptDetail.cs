namespace WarehouseManagement.Domain;

// Chi tiết sản phẩm trên phiếu nhập kho.
// Mỗi dòng ghi số lượng, đơn giá và thành tiền của một sản phẩm.
public class ImportReceiptDetail : BaseEntity
{
    public int ImportReceiptId { get; set; } // FK phiếu nhập
    public ImportReceipt? ImportReceipt { get; set; } // Navigation: phiếu nhập cha

    public int ProductId { get; set; } // FK sản phẩm nhập
    public Product? Product { get; set; } // Navigation: sản phẩm

    public decimal Quantity { get; set; } // Số lượng nhập
    public decimal UnitPrice { get; set; } // Đơn giá nhập
    public decimal LineTotal { get; set; } // Thành tiền dòng = Quantity * UnitPrice

    public string? Note { get; set; } // Ghi chú theo dòng
}
