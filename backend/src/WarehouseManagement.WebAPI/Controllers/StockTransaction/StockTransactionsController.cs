// API lịch sử giao dịch tồn kho (chỉ đọc).
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.WebAPI.Controllers;

// Tra cứu stock transaction; không có API ghi trực tiếp; auth JWT + AdminPortal
public class StockTransactionsController : AdminApiController
{
    private readonly IStockTransactionService _stockTransactionService;

    public StockTransactionsController(IStockTransactionService stockTransactionService)
    {
        _stockTransactionService = stockTransactionService;
    }

    // GET /api/v1/stock-transactions?warehouseId=&productId=&type=Import&referenceType=ImportReceipt
    // Mục đích: liệt kê giao dịch tồn (lọc kho/SP/loại/tham chiếu/ngày)
    // Auth: JWT + AdminPortal
    // Side effect: không (chỉ đọc)
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationQuery query,
        [FromQuery] int? warehouseId = null,
        [FromQuery] int? productId = null,
        [FromQuery] StockTransactionType? type = null,
        [FromQuery] string? referenceType = null,
        [FromQuery] int? referenceId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var result = await _stockTransactionService.GetAllAsync(
            query, warehouseId, productId, type, referenceType, referenceId, fromDate, toDate);
        return Ok(ApiResponse<PagedResult<StockTransactionResponseDto>>.Ok(result));
    }

    // GET /api/v1/stock-transactions/{id}
    // Mục đích: lấy chi tiết một giao dịch tồn
    // Auth: JWT + AdminPortal
    // Side effect: không (chỉ đọc)
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _stockTransactionService.GetByIdAsync(id);
        return Ok(ApiResponse<StockTransactionResponseDto>.Ok(result));
    }
}
