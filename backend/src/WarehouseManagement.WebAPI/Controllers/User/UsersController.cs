// API quản lý người dùng hệ thống (Admin portal).
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Shared;

namespace WarehouseManagement.WebAPI.Controllers;

// CRUD người dùng; kế thừa auth JWT + AdminPortal từ AdminApiController
public class UsersController : AdminApiController
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    // GET /api/v1/users
    // Mục đích: liệt kê người dùng có phân trang
    // Auth: JWT + AdminPortal
    // Side effect: không (chỉ đọc)
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationQuery query)
    {
        var result = await _userService.GetAllAsync(query);
        return Ok(ApiResponse<PagedResult<UserResponseDto>>.Ok(result));
    }

    // GET /api/v1/users/{id}
    // Mục đích: lấy chi tiết một người dùng
    // Auth: JWT + AdminPortal
    // Side effect: không (chỉ đọc)
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _userService.GetByIdAsync(id);
        return Ok(ApiResponse<UserResponseDto>.Ok(result));
    }

    // POST /api/v1/users
    // Mục đích: tạo người dùng mới
    // Auth: JWT + AdminPortal
    // Side effect: ghi DB (user + hash mật khẩu)
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        var result = await _userService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<UserResponseDto>.Ok(result, "Tạo người dùng thành công"));
    }

    // PUT /api/v1/users/{id}
    // Mục đích: cập nhật thông tin người dùng
    // Auth: JWT + AdminPortal
    // Side effect: cập nhật DB
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto)
    {
        var result = await _userService.UpdateAsync(id, dto);
        return Ok(ApiResponse<UserResponseDto>.Ok(result, "Cập nhật người dùng thành công"));
    }

    // DELETE /api/v1/users/{id}
    // Mục đích: xóa người dùng
    // Auth: JWT + AdminPortal
    // Side effect: xóa/gỡ bản ghi trên DB
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _userService.DeleteAsync(id);
        return NoContent();
    }
}
