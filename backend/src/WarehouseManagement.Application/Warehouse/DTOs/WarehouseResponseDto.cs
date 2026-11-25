// Lớp Application — DTO phản hồi thông tin kho hàng.
namespace WarehouseManagement.Application.DTOs;

public class WarehouseResponseDto
{
    // Khóa chính kho
    public int Id { get; set; }

    // Mã kho
    public string Code { get; set; } = string.Empty;

    // Tên kho
    public string Name { get; set; } = string.Empty;

    // Địa chỉ
    public string? Address { get; set; }

    // Số điện thoại
    public string? Phone { get; set; }

    // Người quản lý
    public string? ManagerName { get; set; }

    // Ghi chú
    public string? Note { get; set; }

    // Trạng thái kích hoạt
    public bool IsActive { get; set; }

    /// <summary>Loại kho (General/ColdChain/Controlled/Emergency/MedicalSupply).</summary>
    public string WarehouseType { get; set; } = "General";

    // Thời điểm tạo
    public DateTime CreatedAt { get; set; }

    // Thời điểm cập nhật gần nhất
    public DateTime? UpdatedAt { get; set; }
}
