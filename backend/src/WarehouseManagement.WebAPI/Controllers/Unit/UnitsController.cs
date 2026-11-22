// API quản lý đơn vị tính sản phẩm.
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Shared;

namespace WarehouseManagement.WebAPI.Controllers;

// CRUD đơn vị tính; auth JWT + AdminPortal
public class UnitsController : AdminApiController
{
    private readonly IUnitService _unitService;

    public UnitsController(IUnitService unitService)
    {
        _unitService = unitService;
    }

    // GET /api/v1/units
    // Mục đích: liệt kê đơn vị tính có phân trang
    // Auth: JWT + AdminPortal
    // Side effect: không (chỉ đọc)
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationQuery query)
    {
        var result = await _unitService.GetAllAsync(query);
        return Ok(ApiResponse<PagedResult<UnitResponseDto>>.Ok(result));
    }

    // GET /api/v1/units/{id}
    // Mục đích: lấy chi tiết một đơn vị tính
    // Auth: JWT + AdminPortal
    // Side effect: không (chỉ đọc)
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _unitService.GetByIdAsync(id);
        return Ok(ApiResponse<UnitResponseDto>.Ok(result));
    }

    // POST /api/v1/units
    // Mục đích: tạo đơn vị tính mới
    // Auth: JWT + AdminPortal
    // Side effect: ghi DB
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UnitRequestDto dto)
    {
        var result = await _unitService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<UnitResponseDto>.Ok(result, "Tạo đơn vị tính thành công"));
    }

    // PUT /api/v1/units/{id}
    // Mục đích: cập nhật đơn vị tính
    // Auth: JWT + AdminPortal
    // Side effect: cập nhật DB
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UnitRequestDto dto)
    {
        var result = await _unitService.UpdateAsync(id, dto);
        return Ok(ApiResponse<UnitResponseDto>.Ok(result, "Cập nhật đơn vị tính thành công"));
    }

    // DELETE /api/v1/units/{id}
    // Mục đích: xóa đơn vị tính
    // Auth: JWT + AdminPortal
    // Side effect: xóa bản ghi trên DB
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _unitService.DeleteAsync(id);
        return NoContent();
    }
}
