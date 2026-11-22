// Lớp Application — DTO phản hồi phiếu kiểm kê.
namespace WarehouseManagement.Application.DTOs;

public class StocktakeResponseDto
{
    // Khóa chính phiếu kiểm kê
    public int Id { get; set; }

    // Mã phiếu kiểm kê
    public string Code { get; set; } = string.Empty;

    // Ngày kiểm kê
    public DateTime StocktakeDate { get; set; }

    // Id kho
    public int WarehouseId { get; set; }

    // Mã kho
    public string? WarehouseCode { get; set; }

    // Tên kho
    public string? WarehouseName { get; set; }

    // Trạng thái phiếu
    public string Status { get; set; } = string.Empty;

    // Ghi chú
    public string? Note { get; set; }

    // Tổng số dòng chi tiết
    public int TotalLines { get; set; }

    // Số dòng có chênh lệch tồn
    public int DiffLines { get; set; }

    // Thời điểm xác nhận
    public DateTime? ConfirmedAt { get; set; }

    // Thời điểm hủy
    public DateTime? CancelledAt { get; set; }

    // Chi tiết các dòng kiểm kê
    public List<StocktakeDetailResponseDto> Details { get; set; } = [];

    // Thời điểm tạo
    public DateTime CreatedAt { get; set; }

    // Thời điểm cập nhật gần nhất
    public DateTime? UpdatedAt { get; set; }
}
