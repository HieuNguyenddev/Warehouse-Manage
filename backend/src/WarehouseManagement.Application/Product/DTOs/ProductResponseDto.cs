// Lớp Application — DTO phản hồi thông tin sản phẩm (kèm ảnh và danh mục/đơn vị).
namespace WarehouseManagement.Application.DTOs;

public class ProductResponseDto
{
    // Khóa chính sản phẩm
    public int Id { get; set; }

    // Mã SKU
    public string Sku { get; set; } = string.Empty;

    // Tên sản phẩm
    public string Name { get; set; } = string.Empty;

    // Mô tả
    public string? Description { get; set; }

    // Giá bán
    public decimal Price { get; set; }

    // Ngưỡng tồn tối thiểu mặc định
    public int MinStock { get; set; }

    // Trạng thái kích hoạt
    public bool IsActive { get; set; }

    // Id danh mục
    public int CategoryId { get; set; }

    // Tên danh mục (hiển thị)
    public string? CategoryName { get; set; }

    // Id đơn vị tính
    public int UnitId { get; set; }

    // Tên đơn vị tính
    public string? UnitName { get; set; }

    // Ký hiệu đơn vị tính
    public string? UnitSymbol { get; set; }

    // Danh sách ảnh sản phẩm
    public List<ProductImageDto> Images { get; set; } = [];

    // Thời điểm tạo
    public DateTime CreatedAt { get; set; }

    // Thời điểm cập nhật gần nhất
    public DateTime? UpdatedAt { get; set; }
}
