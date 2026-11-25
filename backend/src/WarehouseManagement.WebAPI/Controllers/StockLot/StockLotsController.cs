using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Shared;

namespace WarehouseManagement.WebAPI.Controllers;

/// <summary>
/// API tồn theo lô / hạn dùng + cảnh báo cận hạn (kho dược BV).
/// </summary>
public class StockLotsController : AdminApiController
{
    private readonly IStockLotService _lots;

    public StockLotsController(IStockLotService lots) => _lots = lots;

    /// <summary>GET /api/v1/stock-lots — danh sách tồn lô còn hàng.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationQuery query,
        [FromQuery] int? warehouseId = null,
        [FromQuery] int? productId = null)
    {
        var result = await _lots.GetPagedAsync(query, warehouseId, productId);
        return Ok(ApiResponse<PagedResult<StockLotResponseDto>>.Ok(result));
    }

    /// <summary>
    /// GET /api/v1/stock-lots/near-expiry?withinDays=90
    /// Cảnh báo thuốc cận hạn / đã hết hạn (Quantity &gt; 0).
    /// </summary>
    [HttpGet("near-expiry")]
    public async Task<IActionResult> GetNearExpiry(
        [FromQuery] int withinDays = 90,
        [FromQuery] int? warehouseId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _lots.GetNearExpiryAsync(withinDays, warehouseId, pageNumber, pageSize);
        return Ok(ApiResponse<PagedResult<StockLotResponseDto>>.Ok(result));
    }
}
