// API phiếu kiểm kê + chi tiết dòng kiểm kê lồng theo phiếu.
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.WebAPI.Controllers;

// Quản lý kiểm kê (Draft → Confirm/Cancel) và CRUD dòng chi tiết; Confirm điều chỉnh tồn; auth JWT + AdminPortal
public class StocktakesController : AdminApiController
{
    private readonly IStocktakeService _stocktakeService;
    private readonly IStocktakeDetailService _stocktakeDetailService;

    public StocktakesController(IStocktakeService stocktakeService, IStocktakeDetailService stocktakeDetailService)
    {
        _stocktakeService = stocktakeService;
        _stocktakeDetailService = stocktakeDetailService;
    }

    // GET /api/v1/stocktakes?warehouseId=&status=Draft
    // Mục đích: liệt kê phiếu kiểm kê
    // Auth: JWT + AdminPortal
    // Side effect: không (chỉ đọc)
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationQuery query,
        [FromQuery] int? warehouseId = null,
        [FromQuery] StocktakeStatus? status = null)
    {
        var result = await _stocktakeService.GetAllAsync(query, warehouseId, status);
        return Ok(ApiResponse<PagedResult<StocktakeResponseDto>>.Ok(result));
    }

    // GET /api/v1/stocktakes/{id}
    // Mục đích: lấy chi tiết phiếu kiểm kê
    // Auth: JWT + AdminPortal
    // Side effect: không (chỉ đọc)
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _stocktakeService.GetByIdAsync(id);
        return Ok(ApiResponse<StocktakeResponseDto>.Ok(result));
    }

    // POST /api/v1/stocktakes
    // Mục đích: tạo phiếu kiểm kê thủ công
    // Auth: JWT + AdminPortal
    // Side effect: ghi DB (chưa đổi tồn)
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] StocktakeRequestDto dto)
    {
        var result = await _stocktakeService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<StocktakeResponseDto>.Ok(result, "Tạo phiếu kiểm kê thành công"));
    }

    // POST /api/v1/stocktakes/auto
    // Mục đích: tạo phiếu kiểm kho tự động từ tồn hiện tại
    // Auth: JWT + AdminPortal
    // Side effect: ghi phiếu + dòng chi tiết vào DB
    [HttpPost("auto")]
    public async Task<IActionResult> CreateAuto([FromBody] AutoStocktakeRequestDto dto)
    {
        var result = await _stocktakeService.CreateAutoAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<StocktakeResponseDto>.Ok(result, "Tạo phiếu kiểm kho tự động thành công"));
    }

    // PUT /api/v1/stocktakes/{id}
    // Mục đích: cập nhật phiếu kiểm kê (thường chỉ Draft)
    // Auth: JWT + AdminPortal
    // Side effect: cập nhật DB
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] StocktakeRequestDto dto)
    {
        var result = await _stocktakeService.UpdateAsync(id, dto);
        return Ok(ApiResponse<StocktakeResponseDto>.Ok(result, "Cập nhật phiếu kiểm kê thành công"));
    }

    // POST /api/v1/stocktakes/{id}/confirm
    // Mục đích: xác nhận kiểm kê
    // Auth: JWT + AdminPortal
    // Side effect: điều chỉnh tồn theo chênh lệch + ghi stock transaction
    [HttpPost("{id:int}/confirm")]
    public async Task<IActionResult> Confirm(int id)
    {
        var result = await _stocktakeService.ConfirmAsync(id);
        return Ok(ApiResponse<StocktakeResponseDto>.Ok(result, "Xác nhận kiểm kê thành công"));
    }

    // POST /api/v1/stocktakes/{id}/cancel
    // Mục đích: hủy phiếu kiểm kê
    // Auth: JWT + AdminPortal
    // Side effect: cập nhật trạng thái (không áp chênh lệch tồn)
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _stocktakeService.CancelAsync(id);
        return Ok(ApiResponse<StocktakeResponseDto>.Ok(result, "Hủy phiếu kiểm kê thành công"));
    }

    // DELETE /api/v1/stocktakes/{id}
    // Mục đích: xóa phiếu kiểm kê (thường chỉ Draft)
    // Auth: JWT + AdminPortal
    // Side effect: xóa phiếu + chi tiết trên DB
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _stocktakeService.DeleteAsync(id);
        return NoContent();
    }

    // ===== Chi tiết kiểm kê theo phiếu =====

    // GET /api/v1/stocktakes/{stocktakeId}/details
    // Mục đích: liệt kê dòng chi tiết của một phiếu (có thể chỉ dòng lệch)
    // Auth: JWT + AdminPortal
    // Side effect: không (chỉ đọc)
    [HttpGet("{stocktakeId:int}/details")]
    public async Task<IActionResult> GetDetails(
        int stocktakeId,
        [FromQuery] PaginationQuery query,
        [FromQuery] bool? onlyDiff = null)
    {
        var result = await _stocktakeDetailService.GetAllAsync(
            query, stocktakeId: stocktakeId, onlyDiff: onlyDiff);
        return Ok(ApiResponse<PagedResult<StocktakeDetailItemDto>>.Ok(result));
    }

    // POST /api/v1/stocktakes/{stocktakeId}/details
    // Mục đích: thêm dòng kiểm kê vào phiếu
    // Auth: JWT + AdminPortal
    // Side effect: ghi DB dòng chi tiết
    [HttpPost("{stocktakeId:int}/details")]
    public async Task<IActionResult> AddDetail(int stocktakeId, [FromBody] StocktakeDetailRequestDto dto)
    {
        var result = await _stocktakeDetailService.AddAsync(stocktakeId, dto);
        return CreatedAtAction(
            nameof(StocktakeDetailsController.GetById),
            "StocktakeDetails",
            new { id = result.Id },
            ApiResponse<StocktakeDetailItemDto>.Ok(result, "Thêm dòng kiểm kê thành công"));
    }

    // PUT /api/v1/stocktakes/{stocktakeId}/details/{detailId}
    // Mục đích: cập nhật số lượng thực tế / ghi chú dòng kiểm kê
    // Auth: JWT + AdminPortal
    // Side effect: cập nhật DB
    [HttpPut("{stocktakeId:int}/details/{detailId:int}")]
    public async Task<IActionResult> UpdateDetail(
        int stocktakeId,
        int detailId,
        [FromBody] StocktakeDetailUpdateDto dto)
    {
        var result = await _stocktakeDetailService.UpdateAsync(stocktakeId, detailId, dto);
        return Ok(ApiResponse<StocktakeDetailItemDto>.Ok(result, "Cập nhật dòng kiểm kê thành công"));
    }

    // DELETE /api/v1/stocktakes/{stocktakeId}/details/{detailId}
    // Mục đích: xóa dòng kiểm kê
    // Auth: JWT + AdminPortal
    // Side effect: xóa bản ghi chi tiết trên DB
    [HttpDelete("{stocktakeId:int}/details/{detailId:int}")]
    public async Task<IActionResult> DeleteDetail(int stocktakeId, int detailId)
    {
        await _stocktakeDetailService.DeleteAsync(stocktakeId, detailId);
        return NoContent();
    }
}
