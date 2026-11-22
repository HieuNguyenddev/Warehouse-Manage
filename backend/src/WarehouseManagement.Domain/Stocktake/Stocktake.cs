namespace WarehouseManagement.Domain;

// Phiếu kiểm kê kho — đối chiếu tồn hệ thống với số đếm thực tế.
// Khi Confirmed sẽ điều chỉnh WarehouseStock theo Difference từng dòng.
public class Stocktake : BaseEntity
{
    public string Code { get; set; } = string.Empty; // Mã phiếu: KK-yyyyMMdd-xxxx
    public DateTime StocktakeDate { get; set; } = DateTime.UtcNow; // Ngày lập phiếu kiểm kê

    public int WarehouseId { get; set; } // FK kho được kiểm kê
    public Warehouse? Warehouse { get; set; } // Navigation: kho kiểm kê

    public StocktakeStatus Status { get; set; } = StocktakeStatus.Draft; // Trạng thái nghiệp vụ phiếu
    public string? Note { get; set; } // Ghi chú kiểm kê

    public DateTime? ConfirmedAt { get; set; } // Thời điểm xác nhận (đã chỉnh tồn)
    public DateTime? CancelledAt { get; set; } // Thời điểm hủy phiếu

    public ICollection<StocktakeDetail> Details { get; set; } = []; // Các dòng kiểm kê theo sản phẩm
}
