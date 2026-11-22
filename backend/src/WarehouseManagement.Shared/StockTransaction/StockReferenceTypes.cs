namespace WarehouseManagement.Shared;

// Giá trị ReferenceType trên StockTransaction — liên kết chứng từ gốc
public static class StockReferenceTypes
{
    public const string ImportReceipt = "ImportReceipt"; // phiếu nhập
    public const string ExportReceipt = "ExportReceipt"; // phiếu xuất
    public const string Stocktake = "Stocktake"; // phiếu kiểm kê
    public const string WarehouseStock = "WarehouseStock"; // điều chỉnh tồn tay
}
