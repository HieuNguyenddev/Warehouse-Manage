// API quản lý vai trò (role) hệ thống.
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Shared;

namespace WarehouseManagement.WebAPI.Controllers;

// CRUD vai trò; auth JWT + AdminPortal
public class RolesController : AdminApiController
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    // GET /api/v1/roles
    // Mục đích: liệt kê vai trò có phân trang
    // Auth: JWT + AdminPortal
    // Side effect: không (chỉ đọc)
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationQuery query)
    {
        var result = await _roleService.GetAllAsync(query);
        return Ok(ApiResponse<PagedResult<RoleResponseDto>>.Ok(result));
    }

    // GET /api/v1/roles/{id}
    // Mục đích: lấy chi tiết một vai trò
    // Auth: JWT + AdminPortal
    // Side effect: không (chỉ đọc)
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _roleService.GetByIdAsync(id);
        return Ok(ApiResponse<RoleResponseDto>.Ok(result));
    }

    // POST /api/v1/roles
    // Mục đích: tạo vai trò mới
    // Auth: JWT + AdminPortal
    // Side effect: ghi DB
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RoleRequestDto dto)
    {
        var result = await _roleService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<RoleResponseDto>.Ok(result, "Tạo vai trò thành công"));
    }

    // PUT /api/v1/roles/{id}
    // Mục đích: cập nhật vai trò
    // Auth: JWT + AdminPortal
    // Side effect: cập nhật DB
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] RoleRequestDto dto)
    {
        var result = await _roleService.UpdateAsync(id, dto);
        return Ok(ApiResponse<RoleResponseDto>.Ok(result, "Cập nhật vai trò thành công"));
    }

    // DELETE /api/v1/roles/{id}
    // Mục đích: xóa vai trò
    // Auth: JWT + AdminPortal
    // Side effect: xóa bản ghi trên DB
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _roleService.DeleteAsync(id);
        return NoContent();
    }
}
