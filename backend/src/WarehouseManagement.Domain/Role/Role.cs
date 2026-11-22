namespace WarehouseManagement.Domain;

/// <summary>
/// VAI TRÒ — dùng để phân quyền.
/// Một Role có nhiều User (quan hệ 1-n).
/// </summary>
public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty; // Ví dụ: "Admin", "Customer"
    public string? Description { get; set; }         // Mô tả quyền

    public ICollection<User> Users { get; set; } = []; // Danh sách user thuộc role này
}
