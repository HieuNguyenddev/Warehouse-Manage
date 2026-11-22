namespace WarehouseManagement.Domain;

// Trạng thái phiếu kiểm kê kho — quyết định đã điều chỉnh tồn hay chưa.
public enum StocktakeStatus
{
    Draft = 0,       // Nháp — chưa điều chỉnh tồn, còn sửa được
    Confirmed = 1,   // Đã xác nhận — đã chỉnh tồn theo số đếm
    Cancelled = 2    // Đã hủy — không còn hiệu lực nghiệp vụ
}
