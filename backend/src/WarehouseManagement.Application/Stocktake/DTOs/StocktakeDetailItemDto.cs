// Lớp Application — DTO dòng kiểm kê kèm thông tin phiếu/kho (báo cáo chênh lệch).
namespace WarehouseManagement.Application.DTOs;

public class StocktakeDetailItemDto
{
    // Khóa chính dòng chi tiết
    public int Id { get; set; }

    // Id phiếu kiểm kê cha
    public int StocktakeId { get; set; }

    // Mã phiếu kiểm kê
    public string? StocktakeCode { get; set; }

    // Trạng thái phiếu kiểm kê
    public string? StocktakeStatus { get; set; }

    // Ngày kiểm kê
    public DateTime StocktakeDate { get; set; }

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

    // Tồn hệ thống
    public decimal SystemQuantity { get; set; }

    // Số lượng đếm
    public decimal CountedQuantity { get; set; }

    // Chênh lệch
    public decimal Difference { get; set; }

    // Có lệch tồn hay không
    public bool HasDifference { get; set; }

    // Ghi chú dòng
    public string? Note { get; set; }

    // Thời điểm tạo dòng
    public DateTime CreatedAt { get; set; }

    // Thời điểm cập nhật gần nhất
    public DateTime? UpdatedAt { get; set; }
}
