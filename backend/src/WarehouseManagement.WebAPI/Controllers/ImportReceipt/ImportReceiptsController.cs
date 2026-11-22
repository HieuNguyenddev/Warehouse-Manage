// API phiếu nhập kho (Draft → Confirm/Cancel).
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.WebAPI.Controllers;

// Quản lý phiếu nhập; Confirm cộng tồn, Cancel hoàn tồn; auth JWT + AdminPortal
public class ImportReceiptsController : AdminApiController
{
    private readonly IImportReceiptService _importReceiptService;

    public ImportReceiptsController(IImportReceiptService importReceiptService)
    {
        _importReceiptService = importReceiptService;
    }

    // GET /api/v1/import-receipts?warehouseId=&supplierId=&status=Draft
    // Mục đích: liệt kê phiếu nhập (lọc kho/NCC/trạng thái)
    // Auth: JWT + AdminPortal
    // Side effect: không (chỉ đọc)
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationQuery query,
        [FromQuery] int? warehouseId = null,
        [FromQuery] int? supplierId = null,
        [FromQuery] ImportReceiptStatus? status = null)
    {
        var result = await _importReceiptService.GetAllAsync(query, warehouseId, supplierId, status);
        return Ok(ApiResponse<PagedResult<ImportReceiptResponseDto>>.Ok(result));
    }

    // GET /api/v1/import-receipts/{id}
    // Mục đích: lấy chi tiết phiếu nhập
    // Auth: JWT + AdminPortal
    // Side effect: không (chỉ đọc)
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _importReceiptService.GetByIdAsync(id);
        return Ok(ApiResponse<ImportReceiptResponseDto>.Ok(result));
    }

    // POST /api/v1/import-receipts
    // Mục đích: tạo phiếu nhập ở trạng thái Draft
    // Auth: JWT + AdminPortal
    // Side effect: ghi DB (chưa đổi tồn)
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ImportReceiptRequestDto dto)
    {
        var result = await _importReceiptService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<ImportReceiptResponseDto>.Ok(result, "Tạo phiếu nhập thành công"));
    }

    // PUT /api/v1/import-receipts/{id}
    // Mục đích: cập nhật phiếu nhập (chỉ Draft)
    // Auth: JWT + AdminPortal
    // Side effect: cập nhật DB (chưa đổi tồn)
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ImportReceiptRequestDto dto)
    {
        var result = await _importReceiptService.UpdateAsync(id, dto);
        return Ok(ApiResponse<ImportReceiptResponseDto>.Ok(result, "Cập nhật phiếu nhập thành công"));
    }

    // POST /api/v1/import-receipts/{id}/confirm
    // Mục đích: xác nhận phiếu nhập
    // Auth: JWT + AdminPortal
    // Side effect: cộng tồn kho + ghi stock transaction
    [HttpPost("{id:int}/confirm")]
    public async Task<IActionResult> Confirm(int id)
    {
        var result = await _importReceiptService.ConfirmAsync(id);
        return Ok(ApiResponse<ImportReceiptResponseDto>.Ok(result, "Xác nhận phiếu nhập thành công"));
    }

    // POST /api/v1/import-receipts/{id}/cancel
    // Mục đích: hủy phiếu nhập
    // Auth: JWT + AdminPortal
    // Side effect: hoàn tồn nếu đã Confirm; cập nhật trạng thái
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _importReceiptService.CancelAsync(id);
        return Ok(ApiResponse<ImportReceiptResponseDto>.Ok(result, "Hủy phiếu nhập thành công"));
    }

    // DELETE /api/v1/import-receipts/{id}
    // Mục đích: xóa phiếu nhập (chỉ Draft)
    // Auth: JWT + AdminPortal
    // Side effect: xóa bản ghi trên DB
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _importReceiptService.DeleteAsync(id);
        return NoContent();
    }
}
