using Microsoft.Extensions.Hosting;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Infrastructure.Storage;

// Lưu ảnh trên đĩa wwwroot/uploads (legacy). Ưu tiên dùng DatabaseFileStorageService khi deploy.
public class LocalFileStorageService : IFileStorageService
{
    private static readonly HashSet<string> AllowedExt = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxBytes = 2 * 1024 * 1024;
    private readonly string _webRoot;

    public LocalFileStorageService(IHostEnvironment env)
    {
        _webRoot = Path.Combine(env.ContentRootPath, "wwwroot");
        Directory.CreateDirectory(Path.Combine(_webRoot, "uploads", "avatars"));
        Directory.CreateDirectory(Path.Combine(_webRoot, "uploads", "products"));
    }

    public Task<string> SaveAvatarAsync(Stream fileStream, string originalFileName, string? contentType = null)
        => SaveImageAsync(fileStream, originalFileName, "avatars", contentType);

    public async Task<string> SaveImageAsync(
        Stream fileStream,
        string originalFileName,
        string folder,
        string? contentType = null)
    {
        folder = string.IsNullOrWhiteSpace(folder) ? "misc" : folder.Trim().ToLowerInvariant();

        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (!AllowedExt.Contains(ext))
            throw new ValidationException("Ảnh chỉ chấp nhận: .jpg, .jpeg, .png, .webp");

        if (fileStream.CanSeek)
            fileStream.Position = 0;

        await using var buffer = new MemoryStream();
        await fileStream.CopyToAsync(buffer);

        if (buffer.Length == 0)
            throw new ValidationException("File ảnh trống.");

        if (buffer.Length > MaxBytes)
            throw new ValidationException("Ảnh tối đa 2MB.");

        Directory.CreateDirectory(Path.Combine(_webRoot, "uploads", folder));

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var relative = $"/uploads/{folder}/{fileName}";
        var fullPath = Path.Combine(_webRoot, "uploads", folder, fileName);

        buffer.Position = 0;
        await using var output = File.Create(fullPath);
        await buffer.CopyToAsync(output);
        await output.FlushAsync();

        return relative;
    }

    public void DeleteFile(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return;

        // Local storage không quản lý URL /api/v1/media
        if (relativePath.Contains("/api/v1/media/", StringComparison.OrdinalIgnoreCase))
            return;

        var fullPath = Path.Combine(
            _webRoot,
            relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }

    public Task<(byte[] Content, string ContentType, string FileName)?> TryGetAsync(Guid publicId)
        => Task.FromResult<(byte[] Content, string ContentType, string FileName)?>(null);
}
