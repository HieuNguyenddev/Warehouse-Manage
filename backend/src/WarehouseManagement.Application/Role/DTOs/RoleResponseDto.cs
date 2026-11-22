// Lớp Application — DTO phản hồi thông tin vai trò.
namespace WarehouseManagement.Application.DTOs;

public class RoleResponseDto
{
    // Khóa chính vai trò
    public int Id { get; set; }

    // Tên vai trò
    public string Name { get; set; } = string.Empty;

    // Mô tả vai trò
    public string? Description { get; set; }

    // Thời điểm tạo
    public DateTime CreatedAt { get; set; }

    // Thời điểm cập nhật gần nhất
    public DateTime? UpdatedAt { get; set; }
}
