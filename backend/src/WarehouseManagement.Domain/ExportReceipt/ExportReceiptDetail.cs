namespace WarehouseManagement.Domain;

// Chi tiết sản phẩm trên phiếu xuất kho.
// Mỗi dòng ghi số lượng, đơn giá và thành tiền của một sản phẩm xuất.
public class ExportReceiptDetail : BaseEntity
{
    public int ExportReceiptId { get; set; } // FK phiếu xuất
    public ExportReceipt? ExportReceipt { get; set; } // Navigation: phiếu xuất cha

    public int ProductId { get; set; } // FK sản phẩm xuất
    public Product? Product { get; set; } // Navigation: sản phẩm

    public decimal Quantity { get; set; } // Số lượng xuất
    public decimal UnitPrice { get; set; } // Đơn giá xuất
    public decimal LineTotal { get; set; } // Thành tiền dòng = Quantity * UnitPrice

    /// <summary>
    /// Số lô ưu tiên xuất (null = hệ thống chọn FEFO — hạn gần nhất trước).
    /// </summary>
    public string? LotNumber { get; set; }

    /// <summary>
    /// JSON phân bổ lô sau Confirm (phục vụ hủy xuất hoàn nguyên đúng lô).
    /// Ví dụ: [{"lot":"LOT-01","qty":5},{"lot":"LOT-02","qty":3}]
    /// </summary>
    public string? LotAllocationsJson { get; set; }

    public string? Note { get; set; } // Ghi chú theo dòng
}
