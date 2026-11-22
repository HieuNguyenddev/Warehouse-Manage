using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Infrastructure.Persistence;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Infrastructure.Storage;

// Lưu ảnh vào SQL Server (MediaAssets) — deploy lại vẫn xem được qua API media
public class DatabaseFileStorageService : IFileStorageService
{
    private static readonly HashSet<string> AllowedExt = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxBytes = 2 * 1024 * 1024;

    private readonly ApplicationDbContext _db;
    private readonly string _webRoot; // vẫn hỗ trợ xóa file /uploads cũ trên đĩa

    public DatabaseFileStorageService(ApplicationDbContext db, IHostEnvironment env)
    {
        _db = db;
        _webRoot = Path.Combine(env.ContentRootPath, "wwwroot");
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

        var publicId = Guid.NewGuid();
        var mime = NormalizeContentType(contentType, ext);

        var asset = new MediaAsset
        {
            PublicId = publicId,
            Folder = folder,
            OriginalFileName = Path.GetFileName(originalFileName),
            ContentType = mime,
            SizeBytes = buffer.Length,
            Content = buffer.ToArray()
        };

        await _db.MediaAssets.AddAsync(asset);
        await _db.SaveChangesAsync();

        // URL ổn định — frontend/proxy gọi API, không phụ thuộc wwwroot khi deploy
        return $"/api/v1/media/{publicId:D}";
    }

    public void DeleteFile(string? relativeOrMediaUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeOrMediaUrl))
            return;

        if (TryParseMediaPublicId(relativeOrMediaUrl, out var publicId))
        {
            var asset = _db.MediaAssets.FirstOrDefault(x => x.PublicId == publicId);
            if (asset is null) return;
            _db.MediaAssets.Remove(asset);
            _db.SaveChanges();
            return;
        }

        // Tương thích đường dẫn /uploads/... cũ trên đĩa
        if (!relativeOrMediaUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            return;

        var fullPath = Path.Combine(
            _webRoot,
            relativeOrMediaUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }

    public async Task<(byte[] Content, string ContentType, string FileName)?> TryGetAsync(Guid publicId)
    {
        var asset = await _db.MediaAssets.AsNoTracking()
            .FirstOrDefaultAsync(x => x.PublicId == publicId);
        if (asset is null) return null;
        return (asset.Content, asset.ContentType, asset.OriginalFileName);
    }

    private static string NormalizeContentType(string? contentType, string ext)
    {
        var ct = contentType?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(ct) && ct.StartsWith("image/"))
            return ct;

        return ext switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };
    }

    private static bool TryParseMediaPublicId(string url, out Guid publicId)
    {
        publicId = Guid.Empty;
        // /api/v1/media/{guid}
        const string marker = "/api/v1/media/";
        var idx = url.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return false;
        var token = url[(idx + marker.Length)..].Trim().Trim('/');
        var slash = token.IndexOf('/');
        if (slash >= 0) token = token[..slash];
        var q = token.IndexOf('?');
        if (q >= 0) token = token[..q];
        return Guid.TryParse(token, out publicId);
    }
}
