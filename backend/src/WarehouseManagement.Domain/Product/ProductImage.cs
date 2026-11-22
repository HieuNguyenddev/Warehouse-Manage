namespace WarehouseManagement.Domain;

// Ảnh sản phẩm — một sản phẩm có nhiều ảnh.
// IsPrimary đánh dấu ảnh đại diện hiển thị chính.
public class ProductImage : BaseEntity
{
    public int ProductId { get; set; } // FK sản phẩm
    public Product? Product { get; set; } // Navigation: sản phẩm sở hữu ảnh

    public string ImageUrl { get; set; } = string.Empty; // Đường dẫn: /uploads/products/xxx.jpg
    public int SortOrder { get; set; } // Thứ tự hiển thị (nhỏ hơn = ưu tiên trước)
    public bool IsPrimary { get; set; } // true = ảnh đại diện chính
}
