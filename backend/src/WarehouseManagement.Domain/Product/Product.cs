namespace WarehouseManagement.Domain;

/// <summary>
/// SẢN PHẨM — hàng hóa trong kho.
///
/// Ghi nhớ cho sinh viên:
/// - Đây là "khuôn dữ liệu" (entity), map với bảng Products trong SQL.
/// - Số lượng tồn THỰC TẾ không nằm ở đây, mà nằm ở WarehouseStock (tồn theo từng kho).
/// - Category / Unit chỉ là thông tin phân loại và đơn vị tính.
/// </summary>
public class Product : BaseEntity
{
    // --- Thông tin cơ bản ---
    public string Sku { get; set; } = string.Empty;   // Mã hàng (unique), ví dụ: SP-001
    public string Name { get; set; } = string.Empty;  // Tên hiển thị
    public string? Description { get; set; }          // Mô tả (có thể null)
    public decimal Price { get; set; }                // Giá bán mặc định
    public int MinStock { get; set; }                 // Ngưỡng cảnh báo tồn thấp (toàn cục)
    public bool IsActive { get; set; } = true;        // false = ngừng bán / không cho nhập-xuất mới

    // --- Khóa ngoại (Foreign Key) ---
    public int CategoryId { get; set; }               // Thuộc danh mục nào?
    public Category? Category { get; set; }           // Navigation: object Category (EF tự join)

    public int UnitId { get; set; }                   // Đơn vị tính: cái, hộp, kg...
    public Unit? Unit { get; set; }                   // Navigation: object Unit

    // --- Quan hệ 1-n (một sản phẩm có nhiều...) ---
    public ICollection<ProductImage> Images { get; set; } = [];
    public ICollection<WarehouseStock> Stocks { get; set; } = [];
    public ICollection<ImportReceiptDetail> ImportDetails { get; set; } = [];
    public ICollection<ExportReceiptDetail> ExportDetails { get; set; } = [];
    public ICollection<StocktakeDetail> StocktakeDetails { get; set; } = [];
    public ICollection<StockTransaction> Transactions { get; set; } = [];
}
