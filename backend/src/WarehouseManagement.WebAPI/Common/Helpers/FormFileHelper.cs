using WarehouseManagement.Shared;

namespace WarehouseManagement.WebAPI.Helpers;

// Kiểm tra IFormFile ảnh từ HTTP multipart/form-data trước khi lưu
public static class FormFileHelper
{
    private static readonly HashSet<string> AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp"
    ];

    public const long DefaultMaxBytes = 2 * 1024 * 1024; // 2MB

    // Validate file upload: có file, đuôi + Content-Type hợp lệ, không vượt dung lượng
    public static void EnsureValidImage(IFormFile? file, long maxBytes = DefaultMaxBytes)
    {
        if (file is null || file.Length <= 0)
            throw new ValidationException("Chưa chọn file ảnh.");

        if (file.Length > maxBytes)
            throw new ValidationException($"Ảnh tối đa {maxBytes / (1024 * 1024)}MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new ValidationException("Ảnh chỉ chấp nhận: .jpg, .jpeg, .png, .webp");

        var contentType = (file.ContentType ?? string.Empty).Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(contentType) && !AllowedContentTypes.Contains(contentType))
            throw new ValidationException("Content-Type ảnh không hợp lệ.");
    }
}
