namespace WarehouseManagement.Domain;

// Đơn vị tính của sản phẩm: cái, thùng, kg...
// Mỗi sản phẩm gắn một Unit để quy đổi số lượng trên chứng từ.
public class Unit : BaseEntity
{
    public string Name { get; set; } = string.Empty; // Tên đầy đủ: Kilogram, Thùng, Cái
    public string Symbol { get; set; } = string.Empty; // Ký hiệu ngắn: kg, thùng, cái
    public string? Description { get; set; } // Ghi chú / mô tả đơn vị
    public bool IsActive { get; set; } = true; // false = không chọn cho sản phẩm mới

    public ICollection<Product> Products { get; set; } = []; // Sản phẩm dùng đơn vị này
}
