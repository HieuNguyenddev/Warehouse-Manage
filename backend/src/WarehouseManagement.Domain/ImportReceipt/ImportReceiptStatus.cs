namespace WarehouseManagement.Domain;

// Trạng thái phiếu nhập kho — quyết định đã cộng tồn hay chưa.
public enum ImportReceiptStatus
{
    Draft = 0,       // Nháp — chưa cộng tồn, còn sửa được
    Confirmed = 1,   // Đã xác nhận — đã cộng tồn kho
    Cancelled = 2    // Đã hủy — không còn hiệu lực nghiệp vụ
}
