namespace WarehouseManagement.Domain;

// Lịch sử biến động tồn kho (nhập / xuất / kiểm kê / chỉnh tay).
// Mỗi bản ghi lưu trước-sau và tham chiếu chứng từ gốc gây ra thay đổi.
public class StockTransaction : BaseEntity
{
    public int WarehouseId { get; set; } // FK kho có biến động
    public Warehouse? Warehouse { get; set; } // Navigation: kho

    public int ProductId { get; set; } // FK sản phẩm có biến động
    public Product? Product { get; set; } // Navigation: sản phẩm

    public StockTransactionType Type { get; set; } // Loại biến động (Import, Export, Stocktake...)

    public decimal QuantityChange { get; set; } // Lượng thay đổi: + nhập, - xuất / giảm
    public decimal QuantityBefore { get; set; } // Tồn trước khi thay đổi
    public decimal QuantityAfter { get; set; } // Tồn sau khi thay đổi

    // Tham chiếu chứng từ gốc
    public string? ReferenceType { get; set; } // Loại chứng từ: ImportReceipt | ExportReceipt | Stocktake | WarehouseStock
    public int? ReferenceId { get; set; } // Id chứng từ gốc
    public string? ReferenceCode { get; set; } // Mã chứng từ gốc (để tra cứu nhanh)

    public string? Note { get; set; } // Ghi chú / lý do biến động
}
