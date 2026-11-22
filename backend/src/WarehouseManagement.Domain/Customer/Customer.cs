namespace WarehouseManagement.Domain;

// Khách hàng — đối tượng nhận hàng xuất kho / hóa đơn.
// Có thể gắn User để đăng nhập (role Customer) theo quan hệ 1-1 tùy chọn.
public class Customer : BaseEntity
{
    public string Name { get; set; } = string.Empty; // Tên khách hàng / công ty
    public string? ContactName { get; set; } // Người liên hệ
    public string? Phone { get; set; } // Số điện thoại
    public string? Email { get; set; } // Email
    public string? Address { get; set; } // Địa chỉ giao / xuất hóa đơn
    public string? TaxCode { get; set; } // Mã số thuế
    public string? Note { get; set; } // Ghi chú nội bộ
    public string? AvatarUrl { get; set; } // Đường dẫn ảnh đại diện: /uploads/avatars/xxx.jpg
    public bool IsActive { get; set; } = true; // false = ngưng giao dịch

    // Liên kết tài khoản đăng nhập (1-1, tùy chọn)
    public int? UserId { get; set; } // FK tới User; null nếu chưa có tài khoản
    public User? User { get; set; } // Navigation: tài khoản đăng nhập

    public ICollection<ExportReceipt> ExportReceipts { get; set; } = []; // Phiếu xuất thuộc khách này
    public ICollection<Invoice> Invoices { get; set; } = []; // Hóa đơn thuộc khách này
}
