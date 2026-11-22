namespace WarehouseManagement.Domain;

// Trạng thái phiếu xuất kho — quyết định đã trừ tồn hay chưa.
public enum ExportReceiptStatus
{
    Draft = 0,       // Nháp — chưa trừ tồn, còn sửa được
    Confirmed = 1,   // Đã xác nhận — đã trừ tồn kho
    Cancelled = 2    // Đã hủy — không còn hiệu lực nghiệp vụ
}
