using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Infrastructure.Persistence;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Infrastructure.Repositories;

/// <summary>
/// Truy vấn và CRUD bảng Suppliers (nhà cung cấp).
/// </summary>
public class SupplierRepository : ISupplierRepository
{
    private readonly ApplicationDbContext _dbContext;

    public SupplierRepository(ApplicationDbContext db)
    {
        _dbContext = db;
    }

    /// <summary>Lấy nhà cung cấp theo Id.</summary>
    public Task<Supplier?> GetByIdAsync(int id)
        => _dbContext.Suppliers.FirstOrDefaultAsync(x => x.Id == id);

    /// <summary>Phân trang NCC; tìm theo tên, liên hệ, SĐT, email, MST.</summary>
    public async Task<PagedResult<Supplier>> GetPagedAsync(string? search, int pageNumber, int pageSize)
    {
        var query = _dbContext.Suppliers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                x.Name.Contains(search) ||
                (x.ContactName != null && x.ContactName.Contains(search)) ||
                (x.Phone != null && x.Phone.Contains(search)) ||
                (x.Email != null && x.Email.Contains(search)) ||
                (x.TaxCode != null && x.TaxCode.Contains(search)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Supplier>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = total
        };
    }

    /// <summary>Kiểm tra tên NCC đã tồn tại.</summary>
    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
    {
        var query = _dbContext.Suppliers.AsNoTracking().Where(x => x.Name == name);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    /// <summary>Kiểm tra Email NCC đã tồn tại.</summary>
    public async Task<bool> ExistsByEmailAsync(string email, int? excludeId = null)
    {
        var query = _dbContext.Suppliers.AsNoTracking().Where(x => x.Email == email);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    /// <summary>Kiểm tra mã số thuế NCC đã tồn tại.</summary>
    public async Task<bool> ExistsByTaxCodeAsync(string taxCode, int? excludeId = null)
    {
        var query = _dbContext.Suppliers.AsNoTracking().Where(x => x.TaxCode == taxCode);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    /// <summary>Thêm nhà cung cấp mới.</summary>
    public Task AddAsync(Supplier supplier) => _dbContext.Suppliers.AddAsync(supplier).AsTask();

    /// <summary>Cập nhật nhà cung cấp.</summary>
    public void Update(Supplier supplier) => _dbContext.Suppliers.Update(supplier);

    /// <summary>Xóa nhà cung cấp.</summary>
    public void Delete(Supplier supplier) => _dbContext.Suppliers.Remove(supplier);

    /// <summary>Lưu thay đổi xuống DB.</summary>
    public Task SaveChangesAsync() => _dbContext.SaveChangesAsync();
}
