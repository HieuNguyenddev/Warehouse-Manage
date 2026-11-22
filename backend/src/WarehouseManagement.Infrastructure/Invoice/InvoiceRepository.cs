using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Infrastructure.Persistence;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Infrastructure.Repositories;

/// <summary>
/// Truy vấn và thêm hóa đơn (kèm chi tiết, kho, phiếu xuất).
/// </summary>
public class InvoiceRepository : IInvoiceRepository
{
    private readonly ApplicationDbContext _dbContext;

    public InvoiceRepository(ApplicationDbContext db)
    {
        _dbContext = db;
    }

    /// <summary>Lấy hóa đơn theo Id (Query đầy đủ navigation).</summary>
    public Task<Invoice?> GetByIdAsync(int id)
        => Query()
            .FirstOrDefaultAsync(x => x.Id == id);

    /// <summary>Lấy hóa đơn theo Id thuộc đúng CustomerId (portal khách).</summary>
    public Task<Invoice?> GetByIdForCustomerAsync(int id, int customerId)
        => Query()
            .FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId);

    /// <summary>Phân trang hóa đơn; lọc theo customerId và search (mã HĐ / tên KH / mã PX).</summary>
    public async Task<PagedResult<Invoice>> GetPagedAsync(
        string? search,
        int? customerId,
        int pageNumber,
        int pageSize)
    {
        var query = Query().AsNoTracking().AsQueryable();

        if (customerId is > 0)
            query = query.Where(x => x.CustomerId == customerId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                x.Code.Contains(search) ||
                x.CustomerName.Contains(search) ||
                (x.ExportReceipt != null && x.ExportReceipt.Code.Contains(search)) ||
                (x.Note != null && x.Note.Contains(search)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Invoice>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = total
        };
    }

    /// <summary>Đếm số hóa đơn có mã bắt đầu bằng prefix (sinh mã tự động).</summary>
    public Task<int> CountByCodePrefixAsync(string prefix)
        => _dbContext.Invoices.AsNoTracking().CountAsync(x => x.Code.StartsWith(prefix));

    /// <summary>Thêm hóa đơn mới.</summary>
    public Task AddAsync(Invoice invoice) => _dbContext.Invoices.AddAsync(invoice).AsTask();

    /// <summary>Lưu thay đổi xuống DB.</summary>
    public Task SaveChangesAsync() => _dbContext.SaveChangesAsync();

    /// <summary>Query gốc: Include Warehouse, ExportReceipt, Details → Product → Unit.</summary>
    private IQueryable<Invoice> Query()
        => _dbContext.Invoices
            .Include(x => x.Warehouse)
            .Include(x => x.ExportReceipt)
            .Include(x => x.Details)
                .ThenInclude(d => d.Product)!
                .ThenInclude(p => p!.Unit);
}
