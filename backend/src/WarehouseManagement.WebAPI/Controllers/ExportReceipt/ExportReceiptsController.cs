// API phiếu xuất kho (Manual Draft → Confirm/Cancel; Store chỉ từ checkout).
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.WebAPI.Controllers;

// Quản lý phiếu xuất; Confirm trừ tồn, Cancel hoàn tồn; auth JWT + AdminPortal
public class ExportReceiptsController : AdminApiController
{
    private readonly IExportReceiptService _exportReceiptService;

    public ExportReceiptsController(IExportReceiptService exportReceiptService)
    {
        _exportReceiptService = exportReceiptService;
    }

    // GET /api/v1/export-receipts?warehouseId=&customerId=&status=Draft&source=Store
    // Mục đích: liệt kê phiếu xuất (lọc kho/KH/trạng thái/nguồn)
    // Auth: JWT + AdminPortal
    // Side effect: không (chỉ đọc)
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationQuery query,
        [FromQuery] int? warehouseId = null,
        [FromQuery] int? customerId = null,
        [FromQuery] ExportReceiptStatus? status = null,
        [FromQuery] ExportReceiptSource? source = null)
    {
        var result = await _exportReceiptService.GetAllAsync(query, warehouseId, customerId, status, source);
        return Ok(ApiResponse<PagedResult<ExportReceiptResponseDto>>.Ok(result));
    }

    // GET /api/v1/export-receipts/{id}
    // Mục đích: lấy chi tiết phiếu xuất
    // Auth: JWT + AdminPortal
    // Side effect: không (chỉ đọc)
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _exportReceiptService.GetByIdAsync(id);
        return Ok(ApiResponse<ExportReceiptResponseDto>.Ok(result));
    }

    // POST /api/v1/export-receipts
    // Mục đích: tạo phiếu xuất Draft (luôn ép Source = Manual; Store chỉ từ checkout)
    // Auth: JWT + AdminPortal
    // Side effect: ghi DB (chưa đổi tồn)
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ExportReceiptRequestDto dto)
    {
        dto.Source = ExportReceiptSource.Manual;
        var result = await _exportReceiptService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<ExportReceiptResponseDto>.Ok(result, "Tạo phiếu xuất thành công"));
    }

    // PUT /api/v1/export-receipts/{id}
    // Mục đích: cập nhật phiếu xuất (chỉ Draft)
    // Auth: JWT + AdminPortal
    // Side effect: cập nhật DB (chưa đổi tồn)
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ExportReceiptRequestDto dto)
    {
        // Admin portal chỉ sửa phiếu Manual (Store đến từ checkout)
        dto.Source = ExportReceiptSource.Manual;
        var result = await _exportReceiptService.UpdateAsync(id, dto);
        return Ok(ApiResponse<ExportReceiptResponseDto>.Ok(result, "Cập nhật phiếu xuất thành công"));
    }

    // POST /api/v1/export-receipts/{id}/confirm
    // Mục đích: xác nhận phiếu xuất
    // Auth: JWT + AdminPortal
    // Side effect: trừ tồn kho + ghi stock transaction
    [HttpPost("{id:int}/confirm")]
    public async Task<IActionResult> Confirm(int id)
    {
        var result = await _exportReceiptService.ConfirmAsync(id);
        return Ok(ApiResponse<ExportReceiptResponseDto>.Ok(result, "Xác nhận phiếu xuất thành công"));
    }

    // POST /api/v1/export-receipts/{id}/cancel
    // Mục đích: hủy phiếu xuất
    // Auth: JWT + AdminPortal
    // Side effect: hoàn tồn nếu đã Confirm; cập nhật trạng thái
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _exportReceiptService.CancelAsync(id);
        return Ok(ApiResponse<ExportReceiptResponseDto>.Ok(result, "Hủy phiếu xuất thành công"));
    }

    // DELETE /api/v1/export-receipts/{id}
    // Mục đích: xóa phiếu xuất (chỉ Draft)
    // Auth: JWT + AdminPortal
    // Side effect: xóa bản ghi trên DB
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _exportReceiptService.DeleteAsync(id);
        return NoContent();
    }
}
