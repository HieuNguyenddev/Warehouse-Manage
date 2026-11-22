// Lớp Application — dịch vụ tra cứu sổ giao dịch tồn kho (chỉ đọc).
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Services;

public class StockTransactionService : IStockTransactionService
{
    private readonly IStockTransactionRepository _transactionRepository;

    public StockTransactionService(IStockTransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    // Lấy một giao dịch tồn theo Id.
    public async Task<StockTransactionResponseDto> GetByIdAsync(int id)
    {
        if (id <= 0)
            throw new ValidationException("Id không hợp lệ.");

        var tx = await _transactionRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Không tìm thấy giao dịch tồn kho");

        return ToResponseDto(tx);
    }

    // Lấy danh sách giao dịch phân trang với các bộ lọc kho/SP/loại/chứng từ/ngày.
    public async Task<PagedResult<StockTransactionResponseDto>> GetAllAsync(
        PaginationQuery query,
        int? warehouseId = null,
        int? productId = null,
        StockTransactionType? type = null,
        string? referenceType = null,
        int? referenceId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var pagedResult = await _transactionRepository.GetPagedAsync(
            query.Search?.Trim(),
            warehouseId,
            productId,
            type,
            referenceType?.Trim(),
            referenceId,
            fromDate,
            toDate,
            query.PageNumber,
            query.PageSize);

        return new PagedResult<StockTransactionResponseDto>
        {
            Items = pagedResult.Items.Select(ToResponseDto).ToList(),
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize,
            TotalRecords = pagedResult.TotalRecords
        };
    }

    // Map entity → DTO phản hồi.
    private static StockTransactionResponseDto ToResponseDto(StockTransaction transaction) => new()
    {
        Id = transaction.Id,
        WarehouseId = transaction.WarehouseId,
        WarehouseCode = transaction.Warehouse?.Code,
        WarehouseName = transaction.Warehouse?.Name,
        ProductId = transaction.ProductId,
        ProductSku = transaction.Product?.Sku,
        ProductName = transaction.Product?.Name,
        UnitSymbol = transaction.Product?.Unit?.Symbol,
        Type = transaction.Type.ToString(),
        QuantityChange = transaction.QuantityChange,
        QuantityBefore = transaction.QuantityBefore,
        QuantityAfter = transaction.QuantityAfter,
        ReferenceType = transaction.ReferenceType,
        ReferenceId = transaction.ReferenceId,
        ReferenceCode = transaction.ReferenceCode,
        Note = transaction.Note,
        CreatedAt = transaction.CreatedAt
    };
}
