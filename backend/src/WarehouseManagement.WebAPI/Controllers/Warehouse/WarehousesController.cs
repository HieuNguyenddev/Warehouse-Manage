// API quản lý kho hàng.
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Shared;

namespace WarehouseManagement.WebAPI.Controllers;

// CRUD kho; auth JWT + AdminPortal
public class WarehousesController : AdminApiController
{
    private readonly IWarehouseService _warehouseService;

    public WarehousesController(IWarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
    }

    // GET /api/v1/warehouses
    // Mục đích: liệt kê kho có phân trang
    // Auth: JWT + AdminPortal
    // Side effect: không (chỉ đọc)
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationQuery query)
    {
        var result = await _warehouseService.GetAllAsync(query);
        return Ok(ApiResponse<PagedResult<WarehouseResponseDto>>.Ok(result));
    }

    // GET /api/v1/warehouses/{id}
    // Mục đích: lấy chi tiết một kho
    // Auth: JWT + AdminPortal
    // Side effect: không (chỉ đọc)
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _warehouseService.GetByIdAsync(id);
        return Ok(ApiResponse<WarehouseResponseDto>.Ok(result));
    }

    // POST /api/v1/warehouses
    // Mục đích: tạo kho mới
    // Auth: JWT + AdminPortal
    // Side effect: ghi DB
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] WarehouseRequestDto dto)
    {
        var result = await _warehouseService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<WarehouseResponseDto>.Ok(result, "Tạo kho hàng thành công"));
    }

    // PUT /api/v1/warehouses/{id}
    // Mục đích: cập nhật thông tin kho
    // Auth: JWT + AdminPortal
    // Side effect: cập nhật DB
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] WarehouseRequestDto dto)
    {
        var result = await _warehouseService.UpdateAsync(id, dto);
        return Ok(ApiResponse<WarehouseResponseDto>.Ok(result, "Cập nhật kho hàng thành công"));
    }

    // DELETE /api/v1/warehouses/{id}
    // Mục đích: xóa kho
    // Auth: JWT + AdminPortal
    // Side effect: xóa bản ghi trên DB
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _warehouseService.DeleteAsync(id);
        return NoContent();
    }
}
