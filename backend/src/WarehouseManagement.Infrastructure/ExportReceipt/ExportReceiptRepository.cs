using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Infrastructure.Persistence;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Infrastructure.Repositories;

/// <summary>
/// Truy vấn và CRUD phiếu xuất kho + chi tiết.
/// </summary>
public class ExportReceiptRepository : IExportReceiptRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ExportReceiptRepository(ApplicationDbContext db)
    {
        _dbContext = db;
    }

    /// <summary>Lấy phiếu xuất theo Id kèm Warehouse, Customer, Invoice, Details → Product → Unit.</summary>
    public Task<ExportReceipt?> GetByIdAsync(int id)
        => _dbContext.ExportReceipts
            .Include(x => x.Warehouse)
            .Include(x => x.Customer)
            .Include(x => x.Invoice)
            .Include(x => x.Details)
                .ThenInclude(d => d.Product)!
                .ThenInclude(p => p!.Unit)
            .FirstOrDefaultAsync(x => x.Id == id);

    /// <summary>
    /// Phân trang phiếu xuất; lọc theo kho, khách, trạng thái, nguồn tạo và search.
    /// </summary>
    public async Task<PagedResult<ExportReceipt>> GetPagedAsync(
        string? search,
        int? warehouseId,
        int? customerId,
        ExportReceiptStatus? status,
        ExportReceiptSource? source,
        int pageNumber,
        int pageSize)
    {
        var query = _dbContext.ExportReceipts
            .AsNoTracking()
            .Include(x => x.Warehouse)
            .Include(x => x.Customer)
            .Include(x => x.Invoice)
            .Include(x => x.Details)
                .ThenInclude(d => d.Product)!
                .ThenInclude(p => p!.Unit)
            .AsQueryable();

        if (warehouseId is > 0)
            query = query.Where(x => x.WarehouseId == warehouseId.Value);

        if (customerId is > 0)
            query = query.Where(x => x.CustomerId == customerId.Value);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (source.HasValue)
            query = query.Where(x => x.Source == source.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                x.Code.Contains(search) ||
                (x.Note != null && x.Note.Contains(search)) ||
                (x.Customer != null && x.Customer.Name.Contains(search)) ||
                (x.Warehouse != null && (x.Warehouse.Name.Contains(search) || x.Warehouse.Code.Contains(search))));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<ExportReceipt>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = total
        };
    }

    /// <summary>Đếm số phiếu có mã bắt đầu bằng prefix (dùng sinh mã tự động).</summary>
    public Task<int> CountByCodePrefixAsync(string prefix)
        => _dbContext.ExportReceipts.AsNoTracking().CountAsync(x => x.Code.StartsWith(prefix));

    /// <summary>Thêm phiếu xuất mới.</summary>
    public Task AddAsync(ExportReceipt receipt) => _dbContext.ExportReceipts.AddAsync(receipt).AsTask();

    /// <summary>Cập nhật phiếu xuất.</summary>
    public void Update(ExportReceipt receipt) => _dbContext.ExportReceipts.Update(receipt);

    /// <summary>Xóa phiếu xuất.</summary>
    public void Delete(ExportReceipt receipt) => _dbContext.ExportReceipts.Remove(receipt);

    /// <summary>Xóa hàng loạt chi tiết phiếu xuất.</summary>
    public void RemoveDetails(IEnumerable<ExportReceiptDetail> details)
        => _dbContext.ExportReceiptDetails.RemoveRange(details);

    /// <summary>Lưu thay đổi xuống DB.</summary>
    public Task SaveChangesAsync() => _dbContext.SaveChangesAsync();
}
