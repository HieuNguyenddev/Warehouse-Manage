// Lớp Application — DTO phản hồi thông tin nhà cung cấp.
namespace WarehouseManagement.Application.DTOs;

public class SupplierResponseDto
{
    // Khóa chính nhà cung cấp
    public int Id { get; set; }

    // Tên nhà cung cấp
    public string Name { get; set; } = string.Empty;

    // Người liên hệ
    public string? ContactName { get; set; }

    // Số điện thoại
    public string? Phone { get; set; }

    // Email
    public string? Email { get; set; }

    // Địa chỉ
    public string? Address { get; set; }

    // Mã số thuế
    public string? TaxCode { get; set; }

    // Ghi chú nội bộ
    public string? Note { get; set; }

    // Trạng thái kích hoạt
    public bool IsActive { get; set; }

    // Thời điểm tạo
    public DateTime CreatedAt { get; set; }

    // Thời điểm cập nhật gần nhất
    public DateTime? UpdatedAt { get; set; }
}
