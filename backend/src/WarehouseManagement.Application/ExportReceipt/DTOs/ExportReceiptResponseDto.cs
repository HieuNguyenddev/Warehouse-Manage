// Lớp Application — DTO phản hồi phiếu xuất kho.
namespace WarehouseManagement.Application.DTOs;

public class ExportReceiptResponseDto
{
    // Khóa chính phiếu xuất
    public int Id { get; set; }

    // Mã phiếu xuất
    public string Code { get; set; } = string.Empty;

    // Ngày phiếu
    public DateTime ReceiptDate { get; set; }

    // Id kho
    public int WarehouseId { get; set; }

    // Mã kho
    public string? WarehouseCode { get; set; }

    // Tên kho
    public string? WarehouseName { get; set; }

    // Id khách hàng
    public int CustomerId { get; set; }

    // Tên khách hàng
    public string? CustomerName { get; set; }

    // Trạng thái phiếu
    public string Status { get; set; } = string.Empty;

    // Nguồn phiếu (Manual / Store...)
    public string Source { get; set; } = "Manual";

    // Mã hóa đơn liên kết (nếu có)
    public string? InvoiceCode { get; set; }

    // Ghi chú
    public string? Note { get; set; }

    // Tổng thành tiền
    public decimal TotalAmount { get; set; }

    // Tổng số lượng
    public decimal TotalQuantity { get; set; }

    // Thời điểm xác nhận
    public DateTime? ConfirmedAt { get; set; }

    // Thời điểm hủy
    public DateTime? CancelledAt { get; set; }

    // Chi tiết các dòng xuất
    public List<ExportReceiptDetailResponseDto> Details { get; set; } = [];

    // Thời điểm tạo
    public DateTime CreatedAt { get; set; }

    // Thời điểm cập nhật gần nhất
    public DateTime? UpdatedAt { get; set; }
}
