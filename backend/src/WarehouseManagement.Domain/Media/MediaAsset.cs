namespace WarehouseManagement.Domain;

// File media lưu trong DB qua API /api/v1/media/{id}
public class MediaAsset : BaseEntity
{
    public Guid PublicId { get; set; } = Guid.NewGuid(); // id công khai trên URL
    public string Folder { get; set; } = "misc"; // avatars | products | ...
    public string OriginalFileName { get; set; } = string.Empty; // tên file gốc khi upload
    public string ContentType { get; set; } = "application/octet-stream"; // MIME (image/jpeg...)
    public long SizeBytes { get; set; } // kích thước
    public byte[] Content { get; set; } = []; // nội dung nhị phân
}
