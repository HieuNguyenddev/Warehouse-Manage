namespace WarehouseManagement.Domain;

/// <summary>
/// LỚP CHA chung cho mọi entity.
///
/// Tại sao cần?
/// - Tránh viết lại Id / CreatedAt / UpdatedAt ở mỗi class.
/// - class Product : BaseEntity  →  Product tự có Id, CreatedAt, UpdatedAt.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }                              // Khóa chính (SQL identity)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Ngày tạo (UTC)
    public DateTime? UpdatedAt { get; set; }                  // Ngày sửa gần nhất (null = chưa sửa)
}
