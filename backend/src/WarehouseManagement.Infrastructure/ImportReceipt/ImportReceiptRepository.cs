using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Infrastructure.Persistence;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Infrastructure.Repositories;

/// <summary>
/// Truy vấn và CRUD phiếu nhập kho + chi tiết.
/// </summary>
public class ImportReceiptRepository : IImportReceiptRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ImportReceiptRepository(ApplicationDbContext db)
    {
        _dbContext = db;
    }

    /// <summary>Lấy phiếu nhập theo Id kèm Warehouse, Supplier, Details → Product → Unit.</summary>
    public Task<ImportReceipt?> GetByIdAsync(int id)
        => _dbContext.ImportReceipts
            .Include(x => x.Warehouse)
            .Include(x => x.Supplier)
            .Include(x => x.Details)
                .ThenInclude(d => d.Product)!
                .ThenInclude(p => p!.Unit)
            .FirstOrDefaultAsync(x => x.Id == id);

    /// <summary>
    /// Phân trang phiếu nhập; lọc theo kho, NCC, trạng thái và search (mã/ghi chú/tên).
    /// </summary>
    public async Task<PagedResult<ImportReceipt>> GetPagedAsync(
        string? search,
        int? warehouseId,
        int? supplierId,
        ImportReceiptStatus? status,
        int pageNumber,
        int pageSize)
    {
        var query = _dbContext.ImportReceipts
            .AsNoTracking()
            .Include(x => x.Warehouse)
            .Include(x => x.Supplier)
            .Include(x => x.Details)
                .ThenInclude(d => d.Product)!
                .ThenInclude(p => p!.Unit)
            .AsQueryable();

        if (warehouseId is > 0)
            query = query.Where(x => x.WarehouseId == warehouseId.Value);

        if (supplierId is > 0)
            query = query.Where(x => x.SupplierId == supplierId.Value);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                x.Code.Contains(search) ||
                (x.PurchaseInvoiceCode != null && x.PurchaseInvoiceCode.Contains(search)) ||
                (x.Note != null && x.Note.Contains(search)) ||
                (x.Supplier != null && x.Supplier.Name.Contains(search)) ||
                (x.Warehouse != null && (x.Warehouse.Name.Contains(search) || x.Warehouse.Code.Contains(search))));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<ImportReceipt>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = total
        };
    }

    /// <summary>Đếm số phiếu có mã bắt đầu bằng prefix (dùng sinh mã tự động).</summary>
    public Task<int> CountByCodePrefixAsync(string prefix)
        => _dbContext.ImportReceipts.AsNoTracking().CountAsync(x => x.Code.StartsWith(prefix));

    /// <summary>Đếm số hóa đơn mua hàng theo prefix HDN-…</summary>
    public Task<int> CountByPurchaseInvoiceCodePrefixAsync(string prefix)
        => _dbContext.ImportReceipts.AsNoTracking()
            .CountAsync(x => x.PurchaseInvoiceCode != null && x.PurchaseInvoiceCode.StartsWith(prefix));

    /// <summary>Thêm phiếu nhập mới.</summary>
    public Task AddAsync(ImportReceipt receipt) => _dbContext.ImportReceipts.AddAsync(receipt).AsTask();

    /// <summary>Cập nhật phiếu nhập.</summary>
    public void Update(ImportReceipt receipt) => _dbContext.ImportReceipts.Update(receipt);

    /// <summary>Xóa phiếu nhập.</summary>
    public void Delete(ImportReceipt receipt) => _dbContext.ImportReceipts.Remove(receipt);

    /// <summary>Xóa hàng loạt chi tiết phiếu nhập.</summary>
    public void RemoveDetails(IEnumerable<ImportReceiptDetail> details)
        => _dbContext.ImportReceiptDetails.RemoveRange(details);

    /// <summary>Lưu thay đổi xuống DB.</summary>
    public Task SaveChangesAsync() => _dbContext.SaveChangesAsync();
}
