namespace WarehouseManagement.Domain;

// Chi tiết dòng hàng trên hóa đơn bán hàng.
// Snapshot SKU/tên/đơn vị để hóa đơn không đổi khi sản phẩm gốc bị sửa sau này.
public class InvoiceDetail : BaseEntity
{
    public int InvoiceId { get; set; } // FK hóa đơn
    public Invoice? Invoice { get; set; } // Navigation: hóa đơn cha

    public int ProductId { get; set; } // FK sản phẩm gốc
    public Product? Product { get; set; } // Navigation: sản phẩm

    public string ProductSku { get; set; } = string.Empty; // SKU snapshot lúc lập hóa đơn
    public string ProductName { get; set; } = string.Empty; // Tên SP snapshot lúc lập hóa đơn
    public string? UnitSymbol { get; set; } // Ký hiệu đơn vị snapshot

    public decimal Quantity { get; set; } // Số lượng trên dòng hóa đơn
    public decimal UnitPrice { get; set; } // Đơn giá bán trên dòng
    public decimal LineTotal { get; set; } // Thành tiền dòng = Quantity * UnitPrice
}
