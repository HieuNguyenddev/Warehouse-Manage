namespace WarehouseManagement.Domain;

// Loại biến động tồn kho — phân loại nguyên nhân thay đổi Quantity.
public enum StockTransactionType
{
    Import = 1,          // Nhập kho (confirm phiếu nhập)
    ImportCancel = 2,    // Hủy phiếu nhập đã confirm → hoàn trừ tồn
    Export = 3,          // Xuất kho (confirm phiếu xuất)
    ExportCancel = 4,    // Hủy phiếu xuất đã confirm → hoàn cộng tồn
    Stocktake = 5,       // Điều chỉnh theo kiểm kê đã xác nhận
    StocktakeCancel = 6, // Hủy kiểm kê → hoàn tồn bằng reverse delta
    Manual = 7           // Chỉnh tay qua WarehouseStocks (có lý do)
}
