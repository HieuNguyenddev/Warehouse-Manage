// Lớp Application — hợp đồng truy cập dữ liệu sổ giao dịch tồn kho.
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Interfaces;

public interface IStockTransactionRepository
{
    // Lấy giao dịch theo Id.
    Task<StockTransaction?> GetByIdAsync(int id);

    // Lấy danh sách giao dịch phân trang; lọc search, kho, SP, loại, chứng từ, ngày.
    Task<PagedResult<StockTransaction>> GetPagedAsync(
        string? search,
        int? warehouseId,
        int? productId,
        StockTransactionType? type,
        string? referenceType,
        int? referenceId,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize);

    // Thêm một giao dịch tồn.
    Task AddAsync(StockTransaction transaction);

    // Thêm hàng loạt giao dịch tồn.
    Task AddRangeAsync(IEnumerable<StockTransaction> transactions);

    // Lưu thay đổi xuống DB.
    Task SaveChangesAsync();
}
