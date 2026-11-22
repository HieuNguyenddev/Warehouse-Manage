namespace WarehouseManagement.Domain;

// Chi tiết kiểm kê từng sản phẩm trong một phiếu Stocktake.
// Difference = CountedQuantity - SystemQuantity; Confirm dùng để chỉnh tồn.
public class StocktakeDetail : BaseEntity
{
    public int StocktakeId { get; set; } // FK phiếu kiểm kê
    public Stocktake? Stocktake { get; set; } // Navigation: phiếu kiểm kê cha

    public int ProductId { get; set; } // FK sản phẩm được đếm
    public Product? Product { get; set; } // Navigation: sản phẩm

    public decimal SystemQuantity { get; set; } // Tồn hệ thống (snapshot lúc lập/sửa dòng Draft)
    public decimal CountedQuantity { get; set; } // Số lượng đếm thực tế
    public decimal Difference { get; set; } // Chênh lệch = CountedQuantity - SystemQuantity

    // Tồn thực tế ngay trước khi Confirm (dùng để hủy kiểm kê bằng reverse delta)
    public decimal? QuantityBeforeConfirm { get; set; } // Snapshot tồn trước xác nhận; null nếu chưa confirm

    public string? Note { get; set; } // Ghi chú theo dòng kiểm kê
}
