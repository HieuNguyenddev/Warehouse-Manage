namespace WarehouseManagement.Domain;

// Nhà cung cấp — đối tác cung ứng hàng nhập kho.
// Mỗi phiếu nhập gắn với một Supplier.
public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty; // Tên nhà cung cấp
    public string? ContactName { get; set; } // Người liên hệ
    public string? Phone { get; set; } // Số điện thoại
    public string? Email { get; set; } // Email liên hệ
    public string? Address { get; set; } // Địa chỉ
    public string? TaxCode { get; set; } // Mã số thuế
    public string? Note { get; set; } // Ghi chú nội bộ
    public bool IsActive { get; set; } = true; // false = ngưng giao dịch

    public ICollection<ImportReceipt> ImportReceipts { get; set; } = []; // Các phiếu nhập từ NCC này
}
