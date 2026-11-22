// Lớp Application — DTO phản hồi một dòng sổ giao dịch tồn kho.
namespace WarehouseManagement.Application.DTOs;

public class StockTransactionResponseDto
{
    // Khóa chính giao dịch
    public int Id { get; set; }

    // Id kho
    public int WarehouseId { get; set; }

    // Mã kho
    public string? WarehouseCode { get; set; }

    // Tên kho
    public string? WarehouseName { get; set; }

    // Id sản phẩm
    public int ProductId { get; set; }

    // SKU sản phẩm
    public string? ProductSku { get; set; }

    // Tên sản phẩm
    public string? ProductName { get; set; }

    // Ký hiệu đơn vị
    public string? UnitSymbol { get; set; }

    // Loại giao dịch (Import/Export/Manual/Stocktake...)
    public string Type { get; set; } = string.Empty;

    // Biến động số lượng (+ nhập / - xuất)
    public decimal QuantityChange { get; set; }

    // Tồn trước giao dịch
    public decimal QuantityBefore { get; set; }

    // Tồn sau giao dịch
    public decimal QuantityAfter { get; set; }

    // Loại chứng từ tham chiếu (ImportReceipt, ExportReceipt...)
    public string? ReferenceType { get; set; }

    // Id chứng từ tham chiếu
    public int? ReferenceId { get; set; }

    // Mã chứng từ tham chiếu
    public string? ReferenceCode { get; set; }

    // Ghi chú giao dịch
    public string? Note { get; set; }

    // Thời điểm ghi sổ
    public DateTime CreatedAt { get; set; }
}
