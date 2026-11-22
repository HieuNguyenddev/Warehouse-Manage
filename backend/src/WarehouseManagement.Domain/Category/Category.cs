namespace WarehouseManagement.Domain;

// Danh mục sản phẩm — nhóm hàng hóa theo loại nghiệp vụ.
// Mỗi sản phẩm thuộc đúng một danh mục đang hoạt động.
public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty; // Tên danh mục
    public string? Description { get; set; } // Mô tả thêm về danh mục
    public bool IsActive { get; set; } = true; // false = ẩn / không dùng cho sản phẩm mới

    public ICollection<Product> Products { get; set; } = []; // Các sản phẩm thuộc danh mục
}
