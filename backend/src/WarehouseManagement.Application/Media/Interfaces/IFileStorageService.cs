// Hợp đồng lưu/xóa file — triển khai DB để mọi môi trường deploy đều xem được ảnh
namespace WarehouseManagement.Application.Interfaces;

public interface IFileStorageService
{
    // Lưu ảnh; trả về URL công khai dạng /api/v1/media/{publicId}
    Task<string> SaveImageAsync(
        Stream fileStream,
        string originalFileName,
        string folder,
        string? contentType = null);

    // Lưu avatar (folder avatars)
    Task<string> SaveAvatarAsync(Stream fileStream, string originalFileName, string? contentType = null)
        => SaveImageAsync(fileStream, originalFileName, "avatars", contentType);

    // Xóa theo URL media hoặc đường dẫn /uploads cũ
    void DeleteFile(string? relativeOrMediaUrl);

    // Đọc nội dung media theo PublicId (phục vụ GET)
    Task<(byte[] Content, string ContentType, string FileName)?> TryGetAsync(Guid publicId);
}
