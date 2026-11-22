// Lớp Application — hợp đồng nghiệp vụ tra cứu sổ giao dịch tồn kho (chỉ đọc).
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Interfaces;

public interface IStockTransactionService
{
    // Lấy một giao dịch tồn theo Id.
    Task<StockTransactionResponseDto> GetByIdAsync(int id);

    // Lấy danh sách giao dịch tồn phân trang; lọc kho, SP, loại, chứng từ, khoảng ngày.
    Task<PagedResult<StockTransactionResponseDto>> GetAllAsync(
        PaginationQuery query,
        int? warehouseId = null,
        int? productId = null,
        StockTransactionType? type = null,
        string? referenceType = null,
        int? referenceId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null);
}
