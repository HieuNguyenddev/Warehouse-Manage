// Lớp Application — DTO phản hồi thông tin đơn vị tính.
namespace WarehouseManagement.Application.DTOs;

public class UnitResponseDto
{
    // Khóa chính đơn vị
    public int Id { get; set; }

    // Tên đơn vị (ví dụ: Thùng, Hộp)
    public string Name { get; set; } = string.Empty;

    // Ký hiệu ngắn (ví dụ: thùng, kg, %)
    public string Symbol { get; set; } = string.Empty;

    // Mô tả đơn vị
    public string? Description { get; set; }

    // Trạng thái kích hoạt
    public bool IsActive { get; set; }

    // Thời điểm tạo
    public DateTime CreatedAt { get; set; }

    // Thời điểm cập nhật gần nhất
    public DateTime? UpdatedAt { get; set; }
}
