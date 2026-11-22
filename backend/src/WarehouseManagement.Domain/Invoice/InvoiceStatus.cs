namespace WarehouseManagement.Domain;

// Trạng thái hóa đơn bán hàng.
public enum InvoiceStatus
{
    Issued = 0,    // Đã phát hành — còn hiệu lực
    Cancelled = 1  // Đã hủy hóa đơn
}
