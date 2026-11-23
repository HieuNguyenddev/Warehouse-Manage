// API quản lý khách hàng (Admin portal).
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Shared;
using WarehouseManagement.WebAPI.Helpers;

namespace WarehouseManagement.WebAPI.Controllers;

// CRUD khách hàng + upload avatar; có thể kèm tạo tài khoản Customer; auth JWT + AdminPortal
public class CustomersController : AdminApiController
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    // GET /api/v1/customers
    // Mục đích: liệt kê khách hàng có phân trang
    // Auth: JWT + AdminPortal
    // Side effect: không (chỉ đọc)
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationQuery query)
    {
        var result = await _customerService.GetAllAsync(query);
        return Ok(ApiResponse<PagedResult<CustomerResponseDto>>.Ok(result));
    }

    // GET /api/v1/customers/{id}
    // Mục đích: lấy chi tiết một khách hàng
    // Auth: JWT + AdminPortal
    // Side effect: không (chỉ đọc)
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _customerService.GetByIdAsync(id);
        return Ok(ApiResponse<CustomerResponseDto>.Ok(result));
    }

    // POST /api/v1/customers
    // Mục đích: tạo khách hàng (có thể kèm username + password → tạo user role Customer)
    // Auth: JWT + AdminPortal
    // Side effect: ghi DB khách hàng (+ user nếu có credentials)
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CustomerRequestDto dto)
    {
        var result = await _customerService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<CustomerResponseDto>.Ok(result, "Tạo khách hàng thành công"));
    }

    // PUT /api/v1/customers/{id}
    // Mục đích: cập nhật thông tin khách hàng
    // Auth: JWT + AdminPortal
    // Side effect: cập nhật DB
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CustomerRequestDto dto)
    {
        var result = await _customerService.UpdateAsync(id, dto);
        return Ok(ApiResponse<CustomerResponseDto>.Ok(result, "Cập nhật khách hàng thành công"));
    }

    // POST /api/v1/customers/{id}/avatar  (multipart: file = IFormFile)
    // Nhận IFormFile → lưu MediaAssets → URL /api/v1/media/{id} (mọi môi trường xem được)
    [HttpPost("{id:int}/avatar")]
    [RequestSizeLimit(2 * 1024 * 1024)]
    // Không gắn [FromForm] trên IFormFile — Swagger/Swashbuckle sẽ lỗi generate
    public async Task<IActionResult> UploadAvatar(int id, IFormFile? file)
    {
        FormFileHelper.EnsureValidImage(file);
        await using var stream = file!.OpenReadStream();
        var result = await _customerService.UploadAvatarAsync(id, stream, file.FileName, file.ContentType);
        return Ok(ApiResponse<CustomerResponseDto>.Ok(result, "Cập nhật avatar thành công"));
    }

    // DELETE /api/v1/customers/{id}
    // Mục đích: xóa khách hàng
    // Auth: JWT + AdminPortal
    // Side effect: xóa bản ghi trên DB
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _customerService.DeleteAsync(id);
        return NoContent();
    }
}
