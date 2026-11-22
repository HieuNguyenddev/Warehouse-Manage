// API tra cứu chi tiết kiểm kê (toàn hệ thống / theo bộ lọc).
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.WebAPI.Controllers;

// Chỉ đọc dòng chi tiết kiểm kê (CRUD lồng nằm ở StocktakesController); auth JWT + AdminPortal
public class StocktakeDetailsController : AdminApiController
{
    private readonly IStocktakeDetailService _stocktakeDetailService;

    public StocktakeDetailsController(IStocktakeDetailService stocktakeDetailService)
    {
        _stocktakeDetailService = stocktakeDetailService;
    }

    // GET /api/v1/stocktake-details?onlyDiff=true&warehouseId=&productId=&status=Confirmed
    // Mục đích: liệt kê dòng kiểm kê theo nhiều bộ lọc
    // Auth: JWT + AdminPortal
    // Side effect: không (chỉ đọc)
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationQuery query,
        [FromQuery] int? stocktakeId = null,
        [FromQuery] int? warehouseId = null,
        [FromQuery] int? productId = null,
        [FromQuery] StocktakeStatus? status = null,
        [FromQuery] bool? onlyDiff = null)
    {
        var result = await _stocktakeDetailService.GetAllAsync(
            query, stocktakeId, warehouseId, productId, status, onlyDiff);
        return Ok(ApiResponse<PagedResult<StocktakeDetailItemDto>>.Ok(result));
    }

    // GET /api/v1/stocktake-details/{id}
    // Mục đích: lấy một dòng chi tiết kiểm kê theo id
    // Auth: JWT + AdminPortal
    // Side effect: không (chỉ đọc)
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _stocktakeDetailService.GetByIdAsync(id);
        return Ok(ApiResponse<StocktakeDetailItemDto>.Ok(result));
    }
}
