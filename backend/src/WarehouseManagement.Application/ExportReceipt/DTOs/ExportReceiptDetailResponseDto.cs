// Lớp Application — DTO một dòng chi tiết trên phiếu xuất (response).
namespace WarehouseManagement.Application.DTOs;

public class ExportReceiptDetailResponseDto
{
    // Khóa chính dòng chi tiết
    public int Id { get; set; }

    // Id sản phẩm
    public int ProductId { get; set; }

    // SKU sản phẩm
    public string? ProductSku { get; set; }

    // Tên sản phẩm
    public string? ProductName { get; set; }

    // Ký hiệu đơn vị
    public string? UnitSymbol { get; set; }

    // Số lượng xuất
    public decimal Quantity { get; set; }

    // Đơn giá xuất
    public decimal UnitPrice { get; set; }

    // Thành tiền dòng
    public decimal LineTotal { get; set; }

    // Ghi chú dòng
    public string? Note { get; set; }
}
