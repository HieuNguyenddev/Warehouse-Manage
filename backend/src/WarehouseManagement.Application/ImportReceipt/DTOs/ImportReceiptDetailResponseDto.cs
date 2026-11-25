// Lớp Application — DTO một dòng chi tiết trên phiếu nhập (response).
namespace WarehouseManagement.Application.DTOs;

public class ImportReceiptDetailResponseDto
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

    // Số lượng nhập
    public decimal Quantity { get; set; }

    // Giá nhập
    public decimal UnitPrice { get; set; }

    // Giá bán (cập nhật Product khi Confirm)
    public decimal SalePrice { get; set; }

    // Thành tiền dòng (theo giá nhập)
    public decimal LineTotal { get; set; }

    public string? LotNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ManufactureDate { get; set; }

    // Ghi chú dòng
    public string? Note { get; set; }
}
