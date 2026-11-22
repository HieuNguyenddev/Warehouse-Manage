using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Shared;
using WarehouseManagement.WebAPI.Helpers;

namespace WarehouseManagement.WebAPI.Controllers;

public class ProductsController : AdminApiController
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>GET /api/v1/products — danh sách + tìm kiếm + lọc category.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationQuery query,
        [FromQuery] int? categoryId = null)
    {
        var result = await _productService.GetAllAsync(query, categoryId);
        return Ok(ApiResponse<PagedResult<ProductResponseDto>>.Ok(result));
    }

    /// <summary>GET /api/v1/products/{id} — chi tiết 1 sản phẩm.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _productService.GetByIdAsync(id);
        return Ok(ApiResponse<ProductResponseDto>.Ok(result));
    }

    /// <summary>POST /api/v1/products — tạo mới.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductRequestDto dto)
    {
        var result = await _productService.CreateAsync(dto);
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            ApiResponse<ProductResponseDto>.Ok(result, "Tạo sản phẩm thành công"));
    }

    /// <summary>PUT /api/v1/products/{id} — cập nhật.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductRequestDto dto)
    {
        var result = await _productService.UpdateAsync(id, dto);
        return Ok(ApiResponse<ProductResponseDto>.Ok(result, "Cập nhật sản phẩm thành công"));
    }

    /// <summary>DELETE /api/v1/products/{id} — xóa.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _productService.DeleteAsync(id);
        return NoContent(); // HTTP 204: thành công, không có body
    }

    /// <summary>
    /// POST /api/v1/products/{id}/images
    /// Nhận file ảnh (multipart/form-data) → Service lưu media → trả URL.
    /// </summary>
    [HttpPost("{id:int}/images")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> AddImages(int id, [FromForm] List<IFormFile> files)
    {
        if (files is null || files.Count == 0)
            return BadRequest(ApiResponse<object>.Fail("Chưa chọn ảnh."));

        var payloads = new List<(Stream Stream, string FileName, string? ContentType)>();
        var opened = new List<Stream>();

        try
        {
            // B1: kiểm tra từng file + mở stream
            foreach (var file in files)
            {
                FormFileHelper.EnsureValidImage(file);
                var stream = file.OpenReadStream();
                opened.Add(stream);
                payloads.Add((stream, file.FileName, file.ContentType));
            }

            // B2: giao cho Service xử lý nghiệp vụ
            var result = await _productService.AddImagesAsync(id, payloads);
            return Ok(ApiResponse<ProductResponseDto>.Ok(result, "Upload ảnh thành công"));
        }
        finally
        {
            // B3: luôn đóng stream (kể cả khi lỗi)
            foreach (var stream in opened)
                await stream.DisposeAsync();
        }
    }

    /// <summary>PUT /api/v1/products/{id}/images/{imageId} — thay 1 ảnh.</summary>
    [HttpPut("{id:int}/images/{imageId:int}")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> ReplaceImage(int id, int imageId, [FromForm] IFormFile? file)
    {
        FormFileHelper.EnsureValidImage(file);
        await using var stream = file!.OpenReadStream();
        var result = await _productService.ReplaceImageAsync(
            id, imageId, stream, file.FileName, file.ContentType);
        return Ok(ApiResponse<ProductResponseDto>.Ok(result, "Thay ảnh thành công"));
    }

    /// <summary>DELETE /api/v1/products/{id}/images/{imageId}</summary>
    [HttpDelete("{id:int}/images/{imageId:int}")]
    public async Task<IActionResult> DeleteImage(int id, int imageId)
    {
        var result = await _productService.DeleteImageAsync(id, imageId);
        return Ok(ApiResponse<ProductResponseDto>.Ok(result, "Xóa ảnh thành công"));
    }

    /// <summary>PUT /api/v1/products/{id}/images/{imageId}/primary</summary>
    [HttpPut("{id:int}/images/{imageId:int}/primary")]
    public async Task<IActionResult> SetPrimaryImage(int id, int imageId)
    {
        var result = await _productService.SetPrimaryImageAsync(id, imageId);
        return Ok(ApiResponse<ProductResponseDto>.Ok(result, "Đặt ảnh chính thành công"));
    }
}
