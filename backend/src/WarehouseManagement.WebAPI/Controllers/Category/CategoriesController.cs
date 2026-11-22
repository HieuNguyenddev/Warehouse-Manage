// API quản lý danh mục sản phẩm.
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Shared;

namespace WarehouseManagement.WebAPI.Controllers;

// CRUD danh mục; auth JWT + AdminPortal
public class CategoriesController : AdminApiController
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    // GET /api/v1/categories
    // Mục đích: liệt kê danh mục có phân trang
    // Auth: JWT + AdminPortal
    // Side effect: không (chỉ đọc)
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationQuery query)
    {
        var result = await _categoryService.GetAllAsync(query);
        return Ok(ApiResponse<PagedResult<CategoryResponseDto>>.Ok(result));
    }

    // GET /api/v1/categories/{id}
    // Mục đích: lấy chi tiết một danh mục
    // Auth: JWT + AdminPortal
    // Side effect: không (chỉ đọc)
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _categoryService.GetByIdAsync(id);
        return Ok(ApiResponse<CategoryResponseDto>.Ok(result));
    }

    // POST /api/v1/categories
    // Mục đích: tạo danh mục mới
    // Auth: JWT + AdminPortal
    // Side effect: ghi DB
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CategoryRequestDto dto)
    {
        var result = await _categoryService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<CategoryResponseDto>.Ok(result, "Tạo danh mục thành công"));
    }

    // PUT /api/v1/categories/{id}
    // Mục đích: cập nhật danh mục
    // Auth: JWT + AdminPortal
    // Side effect: cập nhật DB
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CategoryRequestDto dto)
    {
        var result = await _categoryService.UpdateAsync(id, dto);
        return Ok(ApiResponse<CategoryResponseDto>.Ok(result, "Cập nhật danh mục thành công"));
    }

    // DELETE /api/v1/categories/{id}
    // Mục đích: xóa danh mục
    // Auth: JWT + AdminPortal
    // Side effect: xóa bản ghi trên DB
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _categoryService.DeleteAsync(id);
        return NoContent();
    }
}
