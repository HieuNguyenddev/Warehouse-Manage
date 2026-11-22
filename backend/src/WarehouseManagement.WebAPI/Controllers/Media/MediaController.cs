using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.Interfaces;

namespace WarehouseManagement.WebAPI.Controllers;

// Phục vụ file ảnh đã upload (lưu DB) — AllowAnonymous để <img src> mọi môi trường đều xem được
[ApiController]
[Route("api/v1/media")]
[AllowAnonymous]
public class MediaController : ControllerBase
{
    private readonly IFileStorageService _fileStorage;

    public MediaController(IFileStorageService fileStorage)
    {
        _fileStorage = fileStorage;
    }

    // GET /api/v1/media/{publicId}
    // Trả FileStreamResult với Content-Type đúng để trình duyệt hiển thị ảnh
    [HttpGet("{publicId:guid}")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Get(Guid publicId)
    {
        var file = await _fileStorage.TryGetAsync(publicId);
        if (file is null)
            return NotFound();

        var (content, contentType, fileName) = file.Value;
        return File(content, contentType, enableRangeProcessing: true);
    }
}
