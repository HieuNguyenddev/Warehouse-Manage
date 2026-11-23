namespace WarehouseManagement.Domain;

// Nguồn tạo phiếu xuất — phân biệt xuất tay admin và đơn hàng cửa hàng online.
public enum ExportReceiptSource
{
    Manual = 0, // Tạo thủ công bởi admin/nhân viên kho
    Store = 1   // Phát sinh từ đơn mua online (cổng nhà thuốc BV)
}
