namespace WarehouseManagement.Domain;

// Chi tiết sản phẩm trên phiếu nhập kho.
// Mỗi dòng ghi số lượng, đơn giá và thành tiền của một sản phẩm.
public class ImportReceiptDetail : BaseEntity
{
    public int ImportReceiptId { get; set; } // FK phiếu nhập
    public ImportReceipt? ImportReceipt { get; set; } // Navigation: phiếu nhập cha

    public int ProductId { get; set; } // FK sản phẩm nhập
    public Product? Product { get; set; } // Navigation: sản phẩm

    public decimal Quantity { get; set; } // Số lượng nhập
    public decimal UnitPrice { get; set; } // Giá nhập (giá mua từ NCC)
    /// <summary>Giá bán áp dụng lên Product.Price khi xác nhận phiếu — hiển thị cổng người dùng.</summary>
    public decimal SalePrice { get; set; }
    public decimal LineTotal { get; set; } // Thành tiền dòng = Quantity * UnitPrice (theo giá nhập)

    /// <summary>Số lô nhập kho (bắt buộc với thuốc có HSD).</summary>
    public string? LotNumber { get; set; }

    /// <summary>Hạn dùng của lô.</summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>Ngày sản xuất (tuỳ chọn).</summary>
    public DateTime? ManufactureDate { get; set; }

    public string? Note { get; set; } // Ghi chú theo dòng
}
