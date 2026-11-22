// Lớp Application — DTO checkout cửa hàng và hóa đơn khách hàng.
using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Application.DTOs;

// Một dòng trong giỏ hàng checkout
public class StoreCheckoutItemDto
{
    // Id sản phẩm cần mua
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    // Số lượng mua
    [Range(0.01, 999999)]
    public decimal Quantity { get; set; }
}

// Yêu cầu checkout từ cổng Store
public class StoreCheckoutRequestDto
{
    // Danh sách sản phẩm trong giỏ
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public List<StoreCheckoutItemDto> Items { get; set; } = [];

    // Ghi chú đơn hàng
    [MaxLength(255)]
    public string? Note { get; set; }

    // Địa chỉ giao hàng
    [MaxLength(255)]
    public string? ShippingAddress { get; set; }
}

// Kết quả checkout: hóa đơn + phiếu xuất đã tạo
public class StoreCheckoutResponseDto
{
    // Hóa đơn bán hàng vừa tạo
    public InvoiceResponseDto Invoice { get; set; } = null!;

    // Phiếu xuất kho gắn với đơn
    public ExportReceiptResponseDto ExportReceipt { get; set; } = null!;
}

// Chi tiết một dòng trên hóa đơn
public class InvoiceDetailResponseDto
{
    // Khóa chính dòng hóa đơn
    public int Id { get; set; }

    // Id sản phẩm
    public int ProductId { get; set; }

    // SKU sản phẩm
    public string ProductSku { get; set; } = string.Empty;

    // Tên sản phẩm
    public string ProductName { get; set; } = string.Empty;

    // Ký hiệu đơn vị
    public string? UnitSymbol { get; set; }

    // Số lượng
    public decimal Quantity { get; set; }

    // Đơn giá tại thời điểm bán
    public decimal UnitPrice { get; set; }

    // Thành tiền dòng (= Quantity * UnitPrice)
    public decimal LineTotal { get; set; }
}

// Phản hồi đầy đủ một hóa đơn
public class InvoiceResponseDto
{
    // Khóa chính hóa đơn
    public int Id { get; set; }

    // Mã hóa đơn
    public string Code { get; set; } = string.Empty;

    // Ngày lập hóa đơn
    public DateTime InvoiceDate { get; set; }

    // Id khách hàng
    public int CustomerId { get; set; }

    // Tên khách hàng (snapshot)
    public string CustomerName { get; set; } = string.Empty;

    // SĐT khách (snapshot)
    public string? CustomerPhone { get; set; }

    // Email khách (snapshot)
    public string? CustomerEmail { get; set; }

    // Địa chỉ khách (snapshot)
    public string? CustomerAddress { get; set; }

    // MST khách (snapshot)
    public string? CustomerTaxCode { get; set; }

    // Id phiếu xuất liên kết
    public int ExportReceiptId { get; set; }

    // Mã phiếu xuất liên kết
    public string? ExportReceiptCode { get; set; }

    // Id kho xuất hàng
    public int WarehouseId { get; set; }

    // Mã kho
    public string? WarehouseCode { get; set; }

    // Tên kho
    public string? WarehouseName { get; set; }

    // Tổng tiền hàng (chưa phí ship)
    public decimal SubTotal { get; set; }

    // Phí vận chuyển
    public decimal ShippingFee { get; set; }

    // Tổng thanh toán
    public decimal TotalAmount { get; set; }

    // Tổng số lượng mặt hàng
    public decimal TotalQuantity { get; set; }

    // Trạng thái hóa đơn
    public string Status { get; set; } = string.Empty;

    // Phương thức thanh toán
    public string PaymentMethod { get; set; } = string.Empty;

    // Ghi chú
    public string? Note { get; set; }

    // Các dòng chi tiết
    public List<InvoiceDetailResponseDto> Details { get; set; } = [];

    // Thời điểm tạo
    public DateTime CreatedAt { get; set; }
}
