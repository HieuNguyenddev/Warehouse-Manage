namespace WarehouseManagement.Domain;

// Hóa đơn bán hàng — gắn với phiếu xuất đã xác nhận.
// Lưu snapshot thông tin khách tại thời điểm xuất để giữ lịch sử ổn định.
public class Invoice : BaseEntity
{
    public string Code { get; set; } = string.Empty; // Mã hóa đơn: HD-yyyyMMdd-xxxx
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow; // Ngày phát hành hóa đơn

    public int CustomerId { get; set; } // FK khách hàng
    public Customer? Customer { get; set; } // Navigation: khách hàng

    public int ExportReceiptId { get; set; } // FK phiếu xuất nguồn
    public ExportReceipt? ExportReceipt { get; set; } // Navigation: phiếu xuất gắn hóa đơn

    public int WarehouseId { get; set; } // FK kho xuất hàng trên hóa đơn
    public Warehouse? Warehouse { get; set; } // Navigation: kho

    // Snapshot thông tin khách lúc xuất hóa đơn
    public string CustomerName { get; set; } = string.Empty; // Tên khách (snapshot)
    public string? CustomerPhone { get; set; } // SĐT khách (snapshot)
    public string? CustomerEmail { get; set; } // Email khách (snapshot)
    public string? CustomerAddress { get; set; } // Địa chỉ khách (snapshot)
    public string? CustomerTaxCode { get; set; } // MST khách (snapshot)

    public decimal SubTotal { get; set; } // Tổng tiền hàng (chưa phí ship)
    public decimal ShippingFee { get; set; } // Phí vận chuyển
    public decimal TotalAmount { get; set; } // Tổng thanh toán = SubTotal + ShippingFee
    public decimal TotalQuantity { get; set; } // Tổng số lượng mặt hàng trên hóa đơn

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Issued; // Trạng thái hóa đơn
    public string? Note { get; set; } // Ghi chú hóa đơn
    public string PaymentMethod { get; set; } = "COD"; // Phương thức thanh toán (mặc định COD)

    public ICollection<InvoiceDetail> Details { get; set; } = []; // Các dòng chi tiết hóa đơn
}
