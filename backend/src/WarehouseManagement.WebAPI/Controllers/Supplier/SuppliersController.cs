// API quản lý nhà cung cấp.
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Shared;

namespace WarehouseManagement.WebAPI.Controllers;

// CRUD nhà cung cấp; auth JWT + AdminPortal
public class SuppliersController : AdminApiController
{
    private readonly ISupplierService _supplierService;

    public SuppliersController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    // GET /api/v1/suppliers
    // Mục đích: liệt kê nhà cung cấp có phân trang
    // Auth: JWT + AdminPortal
    // Side effect: không (chỉ đọc)
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationQuery query)
    {
        var result = await _supplierService.GetAllAsync(query);
        return Ok(ApiResponse<PagedResult<SupplierResponseDto>>.Ok(result));
    }

    // GET /api/v1/suppliers/{id}
    // Mục đích: lấy chi tiết một nhà cung cấp
    // Auth: JWT + AdminPortal
    // Side effect: không (chỉ đọc)
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _supplierService.GetByIdAsync(id);
        return Ok(ApiResponse<SupplierResponseDto>.Ok(result));
    }

    // POST /api/v1/suppliers
    // Mục đích: tạo nhà cung cấp mới
    // Auth: JWT + AdminPortal
    // Side effect: ghi DB
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SupplierRequestDto dto)
    {
        var result = await _supplierService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<SupplierResponseDto>.Ok(result, "Tạo nhà cung cấp thành công"));
    }

    // PUT /api/v1/suppliers/{id}
    // Mục đích: cập nhật nhà cung cấp
    // Auth: JWT + AdminPortal
    // Side effect: cập nhật DB
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] SupplierRequestDto dto)
    {
        var result = await _supplierService.UpdateAsync(id, dto);
        return Ok(ApiResponse<SupplierResponseDto>.Ok(result, "Cập nhật nhà cung cấp thành công"));
    }

    // DELETE /api/v1/suppliers/{id}
    // Mục đích: xóa nhà cung cấp
    // Auth: JWT + AdminPortal
    // Side effect: xóa bản ghi trên DB
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _supplierService.DeleteAsync(id);
        return NoContent();
    }
}
