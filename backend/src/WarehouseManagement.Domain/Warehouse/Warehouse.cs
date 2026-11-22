namespace WarehouseManagement.Domain;

// Kho hàng — nơi lưu trữ vật lý của sản phẩm.
// Mọi phiếu nhập/xuất/kiểm kê và tồn kho đều gắn với một Warehouse.
public class Warehouse : BaseEntity
{
    public string Code { get; set; } = string.Empty; // Mã kho unique (vd: WH01)
    public string Name { get; set; } = string.Empty; // Tên kho
    public string? Address { get; set; } // Địa chỉ kho
    public string? Phone { get; set; } // Số điện thoại liên hệ kho
    public string? ManagerName { get; set; } // Tên thủ kho / quản lý
    public string? Note { get; set; } // Ghi chú nội bộ
    public bool IsActive { get; set; } = true; // false = kho ngưng hoạt động

    public ICollection<WarehouseStock> Stocks { get; set; } = []; // Tồn từng sản phẩm trong kho
    public ICollection<ImportReceipt> ImportReceipts { get; set; } = []; // Phiếu nhập vào kho này
    public ICollection<ExportReceipt> ExportReceipts { get; set; } = []; // Phiếu xuất từ kho này
    public ICollection<Invoice> Invoices { get; set; } = []; // Hóa đơn gắn kho xuất
    public ICollection<Stocktake> Stocktakes { get; set; } = []; // Phiếu kiểm kê của kho
    public ICollection<StockTransaction> Transactions { get; set; } = []; // Biến động tồn tại kho
}
