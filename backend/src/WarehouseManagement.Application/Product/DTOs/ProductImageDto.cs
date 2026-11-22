// Lớp Application — DTO ảnh sản phẩm trong phản hồi Product.
namespace WarehouseManagement.Application.DTOs;

public class ProductImageDto
{
    // Khóa chính ảnh
    public int Id { get; set; }

    // Đường dẫn tương đối file ảnh
    public string ImageUrl { get; set; } = string.Empty;

    // Thứ tự sắp xếp hiển thị
    public int SortOrder { get; set; }

    // Có phải ảnh chính không
    public bool IsPrimary { get; set; }
}
