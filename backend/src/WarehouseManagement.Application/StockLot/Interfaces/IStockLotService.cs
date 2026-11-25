using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Interfaces;

public interface IStockLotService
{
    Task<PagedResult<StockLotResponseDto>> GetPagedAsync(
        PaginationQuery query,
        int? warehouseId = null,
        int? productId = null);

    Task<PagedResult<StockLotResponseDto>> GetNearExpiryAsync(
        int withinDays = 90,
        int? warehouseId = null,
        int pageNumber = 1,
        int pageSize = 20);

    /// <summary>Cộng tồn lô khi xác nhận phiếu nhập.</summary>
    Task AddFromImportAsync(
        int warehouseId,
        int productId,
        string lotNumber,
        DateTime? expiryDate,
        DateTime? manufactureDate,
        decimal quantity,
        int? importReceiptDetailId);

    /// <summary>Trừ tồn lô khi hủy phiếu nhập (theo số lô).</summary>
    Task DeductExactLotAsync(int warehouseId, int productId, string lotNumber, decimal quantity);

    /// <summary>
    /// Trừ theo FEFO (hoặc đúng số lô nếu preferLot được chỉ định).
    /// Trả về JSON phân bổ lô để lưu trên dòng xuất.
    /// </summary>
    Task<string> DeductForExportAsync(
        int warehouseId,
        int productId,
        decimal quantity,
        string? preferLot);

    /// <summary>Hoàn nguyên lô khi hủy phiếu xuất (từ LotAllocationsJson).</summary>
    Task RestoreFromAllocationsAsync(int warehouseId, int productId, string? lotAllocationsJson);
}
