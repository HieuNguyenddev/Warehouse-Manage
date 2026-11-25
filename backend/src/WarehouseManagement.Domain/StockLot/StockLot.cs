namespace WarehouseManagement.Domain;

/// <summary>
/// Tồn theo lô / hạn dùng (FEFO) trong một kho.
/// Mỗi lần nhập (Confirm) tạo hoặc cộng dồn một dòng StockLot;
/// xuất Confirm trừ theo hạn gần nhất trước.
/// </summary>
public class StockLot : BaseEntity
{
    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    /// <summary>Số lô sản xuất / số lô NCC (vd: LOT-2026-001).</summary>
    public string LotNumber { get; set; } = string.Empty;

    /// <summary>Hạn dùng (null = không theo dõi HSD — vật tư không hạn).</summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>Ngày sản xuất (tuỳ chọn).</summary>
    public DateTime? ManufactureDate { get; set; }

    /// <summary>Số lượng còn lại của lô trong kho.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Dòng phiếu nhập nguồn (nếu có) — phục vụ truy xuất / hủy nhập.</summary>
    public int? ImportReceiptDetailId { get; set; }
}
