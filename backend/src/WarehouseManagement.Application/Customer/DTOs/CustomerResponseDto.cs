// Lớp Application — DTO phản hồi thông tin khách hàng (kèm tài khoản nếu có).
namespace WarehouseManagement.Application.DTOs;

public class CustomerResponseDto
{
    // Khóa chính khách hàng
    public int Id { get; set; }

    // Tên khách hàng / công ty
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

    // Đường dẫn tương đối ảnh đại diện
    public string? AvatarUrl { get; set; }

    // Trạng thái kích hoạt
    public bool IsActive { get; set; }

    // Id tài khoản User liên kết (nếu có)
    public int? UserId { get; set; }

    // Username của tài khoản liên kết
    public string? Username { get; set; }

    // Tên vai trò của tài khoản liên kết
    public string? RoleName { get; set; }

    // Thời điểm tạo
    public DateTime CreatedAt { get; set; }

    // Thời điểm cập nhật gần nhất
    public DateTime? UpdatedAt { get; set; }
}
