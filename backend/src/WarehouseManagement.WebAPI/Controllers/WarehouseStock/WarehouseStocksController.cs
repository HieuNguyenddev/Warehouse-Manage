// API tồn kho theo kho (xem, khởi tạo, điều chỉnh, ngưỡng min).
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Shared;

namespace WarehouseManagement.WebAPI.Controllers;

// Tra cứu/điều chỉnh tồn; route kebab: warehouse-stocks; auth JWT + AdminPortal
public class WarehouseStocksController : AdminApiController
{
    private readonly IWarehouseStockService _stockService;

    public WarehouseStocksController(IWarehouseStockService stockService)
    {
        _stockService = stockService;
    }

    // GET /api/v1/warehouse-stocks/summary?warehouseId=
    // Mục đích: tổng hợp tồn (số SKU, tồn thấp, hết hàng, ...)
    // Auth: JWT + AdminPortal
    // Side effect: không (chỉ đọc)
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] int? warehouseId = null)
    {
        var result = await _stockService.GetSummaryAsync(warehouseId);
        return Ok(ApiResponse<WarehouseStockSummaryDto>.Ok(result));
    }

    // GET /api/v1/warehouse-stocks?warehouseId=&productId=&lowStockOnly=&outOfStockOnly=&search=
    // Mục đích: liệt kê tồn kho có lọc
    // Auth: JWT + AdminPortal
    // Side effect: không (chỉ đọc)
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationQuery query,
        [FromQuery] int? warehouseId = null,
        [FromQuery] int? productId = null,
        [FromQuery] bool? lowStockOnly = null,
        [FromQuery] bool? outOfStockOnly = null)
    {
        var result = await _stockService.GetAllAsync(query, warehouseId, productId, lowStockOnly, outOfStockOnly);
        return Ok(ApiResponse<PagedResult<WarehouseStockResponseDto>>.Ok(result));
    }

    // GET /api/v1/warehouse-stocks/{id}
    // Mục đích: lấy chi tiết một dòng tồn
    // Auth: JWT + AdminPortal
    // Side effect: không (chỉ đọc)
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _stockService.GetByIdAsync(id);
        return Ok(ApiResponse<WarehouseStockResponseDto>.Ok(result));
    }

    // POST /api/v1/warehouse-stocks
    // Mục đích: khởi tạo tồn đầu kỳ
    // Auth: JWT + AdminPortal
    // Side effect: ghi tồn + có thể ghi transaction Opening
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] WarehouseStockRequestDto dto)
    {
        var result = await _stockService.CreateOpeningAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<WarehouseStockResponseDto>.Ok(result, "Khởi tạo tồn đầu kỳ thành công"));
    }

    // POST /api/v1/warehouse-stocks/{id}/adjust
    // Mục đích: điều chỉnh tồn có lý do
    // Auth: JWT + AdminPortal
    // Side effect: cập nhật số lượng tồn + ghi stock transaction Adjust
    [HttpPost("{id:int}/adjust")]
    public async Task<IActionResult> Adjust(int id, [FromBody] WarehouseStockAdjustDto dto)
    {
        var result = await _stockService.AdjustAsync(id, dto);
        return Ok(ApiResponse<WarehouseStockResponseDto>.Ok(result, "Điều chỉnh tồn thành công"));
    }

    // PUT /api/v1/warehouse-stocks/{id}/min
    // Mục đích: cập nhật ngưỡng cảnh báo min theo kho
    // Auth: JWT + AdminPortal
    // Side effect: cập nhật DB (không đổi số lượng tồn)
    [HttpPut("{id:int}/min")]
    public async Task<IActionResult> UpdateMin(int id, [FromBody] WarehouseStockMinDto dto)
    {
        var result = await _stockService.UpdateMinAsync(id, dto);
        return Ok(ApiResponse<WarehouseStockResponseDto>.Ok(result, "Cập nhật ngưỡng min thành công"));
    }

    // PUT /api/v1/warehouse-stocks/{id}
    // Mục đích: cập nhật tồn (API cũ; ưu tiên dùng /adjust)
    // Auth: JWT + AdminPortal
    // Side effect: cập nhật tồn trên DB
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] WarehouseStockUpdateDto dto)
    {
        var result = await _stockService.UpdateAsync(id, dto);
        return Ok(ApiResponse<WarehouseStockResponseDto>.Ok(result, "Cập nhật tồn kho thành công"));
    }
}
