// Lớp Application — DTO phản hồi phiếu nhập kho.
namespace WarehouseManagement.Application.DTOs;

public class ImportReceiptResponseDto
{
    // Khóa chính phiếu nhập
    public int Id { get; set; }

    // Mã phiếu nhập
    public string Code { get; set; } = string.Empty;

    // Ngày phiếu
    public DateTime ReceiptDate { get; set; }

    // Id kho
    public int WarehouseId { get; set; }

    // Mã kho
    public string? WarehouseCode { get; set; }

    // Tên kho
    public string? WarehouseName { get; set; }

    // Id nhà cung cấp
    public int SupplierId { get; set; }

    // Tên nhà cung cấp
    public string? SupplierName { get; set; }

    // Trạng thái phiếu (Draft/Confirmed/Cancelled...)
    public string Status { get; set; } = string.Empty;

    // Ghi chú
    public string? Note { get; set; }

    /// <summary>Tổng giá mua hàng = Σ (SL × đơn giá mua).</summary>
    public decimal GoodsAmount { get; set; }

    /// <summary>Chi phí vận chuyển.</summary>
    public decimal ShippingFee { get; set; }

    /// <summary>Chiết khấu thương mại.</summary>
    public decimal TradeDiscount { get; set; }

    /// <summary>Thuế không được hoàn lại.</summary>
    public decimal NonRefundableTax { get; set; }

    /// <summary>Tổng thanh toán = Giá mua − CK + VC + Thuế KĐHT.</summary>
    public decimal TotalAmount { get; set; }

    // Tổng số lượng
    public decimal TotalQuantity { get; set; }

    /// <summary>Mã hóa đơn mua hàng (sau khi xác nhận).</summary>
    public string? PurchaseInvoiceCode { get; set; }

    // Snapshot NCC (in hóa đơn)
    public string? SupplierTaxCode { get; set; }
    public string? SupplierAddress { get; set; }
    public string? SupplierPhone { get; set; }
    public string? SupplierEmail { get; set; }

    // Thời điểm xác nhận
    public DateTime? ConfirmedAt { get; set; }

    // Thời điểm hủy
    public DateTime? CancelledAt { get; set; }

    // Chi tiết các dòng nhập
    public List<ImportReceiptDetailResponseDto> Details { get; set; } = [];

    // Thời điểm tạo
    public DateTime CreatedAt { get; set; }

    // Thời điểm cập nhật gần nhất
    public DateTime? UpdatedAt { get; set; }
}
