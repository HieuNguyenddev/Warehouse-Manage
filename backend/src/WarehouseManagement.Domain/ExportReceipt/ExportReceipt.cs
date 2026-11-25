namespace WarehouseManagement.Domain;

// Phiếu xuất kho — chứng từ ghi nhận hàng xuất cho khách hàng.
// Chỉ khi Confirmed mới trừ tồn; có thể sinh hóa đơn gắn kèm.
public class ExportReceipt : BaseEntity
{
    public string Code { get; set; } = string.Empty; // Mã phiếu: PX-yyyyMMdd-xxxx
    public DateTime ReceiptDate { get; set; } = DateTime.UtcNow; // Ngày lập phiếu xuất

    public int WarehouseId { get; set; } // FK kho xuất hàng
    public Warehouse? Warehouse { get; set; } // Navigation: kho xuất

    public int CustomerId { get; set; } // FK khách hàng nhận hàng
    public Customer? Customer { get; set; } // Navigation: khách hàng

    public ExportReceiptStatus Status { get; set; } = ExportReceiptStatus.Draft; // Trạng thái nghiệp vụ phiếu

    // Manual = tạo tay admin; Store = đơn từ cổng nhà thuốc BV
    public ExportReceiptSource Source { get; set; } = ExportReceiptSource.Manual; // Nguồn tạo phiếu xuất

    /// <summary>Mục đích xuất: lĩnh khoa / cấp BN / điều chuyển / hủy / trả NCC.</summary>
    public ExportIssuePurpose IssuePurpose { get; set; } = ExportIssuePurpose.DepartmentIssue;

    public string? Note { get; set; } // Ghi chú phiếu xuất

    public DateTime? ConfirmedAt { get; set; } // Thời điểm xác nhận (trừ tồn)
    public DateTime? CancelledAt { get; set; } // Thời điểm hủy phiếu

    public ICollection<ExportReceiptDetail> Details { get; set; } = []; // Các dòng sản phẩm xuất
    public Invoice? Invoice { get; set; } // Hóa đơn gắn phiếu xuất (1-1, nếu có)
}
