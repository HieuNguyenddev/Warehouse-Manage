// Lớp Application — DTO phản hồi thông tin danh mục sản phẩm.
namespace WarehouseManagement.Application.DTOs;

public class CategoryResponseDto
{
    // Khóa chính danh mục
    public int Id { get; set; }

    // Tên danh mục
    public string Name { get; set; } = string.Empty;

    // Mô tả danh mục
    public string? Description { get; set; }

    // Trạng thái kích hoạt
    public bool IsActive { get; set; }

    // Thời điểm tạo
    public DateTime CreatedAt { get; set; }

    // Thời điểm cập nhật gần nhất
    public DateTime? UpdatedAt { get; set; }
}
