namespace WarehouseManagement.Domain;

// Phiếu nhập kho — chứng từ ghi nhận hàng vào từ nhà cung cấp.
// Chỉ khi Confirmed mới cộng tồn WarehouseStock và ghi StockTransaction.
public class ImportReceipt : BaseEntity
{
    public string Code { get; set; } = string.Empty; // Mã phiếu: PN-yyyyMMdd-xxxx
    public DateTime ReceiptDate { get; set; } = DateTime.UtcNow; // Ngày lập phiếu nhập

    public int WarehouseId { get; set; } // FK kho nhận hàng
    public Warehouse? Warehouse { get; set; } // Navigation: kho nhận

    public int SupplierId { get; set; } // FK nhà cung cấp
    public Supplier? Supplier { get; set; } // Navigation: nhà cung cấp

    public ImportReceiptStatus Status { get; set; } = ImportReceiptStatus.Draft; // Trạng thái nghiệp vụ phiếu
    public string? Note { get; set; } // Ghi chú phiếu nhập

    /// <summary>Chi phí vận chuyển (cộng vào tổng thanh toán).</summary>
    public decimal ShippingFee { get; set; }

    /// <summary>Chiết khấu thương mại (trừ khỏi giá mua hàng).</summary>
    public decimal TradeDiscount { get; set; }

    /// <summary>Thuế không được hoàn lại (cộng vào tổng thanh toán).</summary>
    public decimal NonRefundableTax { get; set; }

    /// <summary>Mã hóa đơn mua hàng phát hành khi xác nhận (HDN-…).</summary>
    public string? PurchaseInvoiceCode { get; set; }

    public DateTime? ConfirmedAt { get; set; } // Thời điểm xác nhận (cộng tồn)
    public DateTime? CancelledAt { get; set; } // Thời điểm hủy phiếu

    public ICollection<ImportReceiptDetail> Details { get; set; } = []; // Các dòng sản phẩm nhập
}
