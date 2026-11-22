// Lớp Application — DTO một dòng chi tiết trên phiếu kiểm kê (response).
namespace WarehouseManagement.Application.DTOs;

public class StocktakeDetailResponseDto
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

    // Tồn trên hệ thống tại thời điểm lập/cập nhật dòng
    public decimal SystemQuantity { get; set; }

    // Số lượng thực tế đếm được
    public decimal CountedQuantity { get; set; }

    // Chênh lệch (= CountedQuantity - SystemQuantity)
    public decimal Difference { get; set; }

    // Tồn trước khi xác nhận (snapshot khi confirm)
    public decimal? QuantityBeforeConfirm { get; set; }

    // Ghi chú dòng
    public string? Note { get; set; }
}
