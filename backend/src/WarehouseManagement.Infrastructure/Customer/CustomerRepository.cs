using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Infrastructure.Persistence;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Infrastructure.Repositories;

/// <summary>
/// Truy vấn và CRUD bảng Customers (kèm User/Role nếu có liên kết).
/// </summary>
public class CustomerRepository : ICustomerRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CustomerRepository(ApplicationDbContext db)
    {
        _dbContext = db;
    }

    /// <summary>Lấy khách hàng theo Id, Include User → Role.</summary>
    public Task<Customer?> GetByIdAsync(int id)
        => _dbContext.Customers
            .Include(x => x.User)!.ThenInclude(u => u!.Role)
            .FirstOrDefaultAsync(x => x.Id == id);

    /// <summary>Lấy khách hàng theo UserId (portal khách đăng nhập).</summary>
    public Task<Customer?> GetByUserIdAsync(int userId)
        => _dbContext.Customers
            .Include(x => x.User)!.ThenInclude(u => u!.Role)
            .FirstOrDefaultAsync(x => x.UserId == userId);

    /// <summary>Phân trang khách; tìm theo tên, liên hệ, SĐT, email, MST, username.</summary>
    public async Task<PagedResult<Customer>> GetPagedAsync(string? search, int pageNumber, int pageSize)
    {
        var query = _dbContext.Customers
            .AsNoTracking()
            .Include(x => x.User)!.ThenInclude(u => u!.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                x.Name.Contains(search) ||
                (x.ContactName != null && x.ContactName.Contains(search)) ||
                (x.Phone != null && x.Phone.Contains(search)) ||
                (x.Email != null && x.Email.Contains(search)) ||
                (x.TaxCode != null && x.TaxCode.Contains(search)) ||
                (x.User != null && x.User.Username.Contains(search)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Customer>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = total
        };
    }

    /// <summary>Kiểm tra tên khách đã tồn tại.</summary>
    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
    {
        var query = _dbContext.Customers.AsNoTracking().Where(x => x.Name == name);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    /// <summary>Kiểm tra Email khách đã tồn tại.</summary>
    public async Task<bool> ExistsByEmailAsync(string email, int? excludeId = null)
    {
        var query = _dbContext.Customers.AsNoTracking().Where(x => x.Email == email);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    /// <summary>Kiểm tra mã số thuế khách đã tồn tại.</summary>
    public async Task<bool> ExistsByTaxCodeAsync(string taxCode, int? excludeId = null)
    {
        var query = _dbContext.Customers.AsNoTracking().Where(x => x.TaxCode == taxCode);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    /// <summary>Thêm khách hàng mới.</summary>
    public Task AddAsync(Customer customer) => _dbContext.Customers.AddAsync(customer).AsTask();

    /// <summary>Cập nhật khách hàng.</summary>
    public void Update(Customer customer) => _dbContext.Customers.Update(customer);

    /// <summary>Xóa khách hàng.</summary>
    public void Delete(Customer customer) => _dbContext.Customers.Remove(customer);

    /// <summary>Lưu thay đổi xuống DB.</summary>
    public Task SaveChangesAsync() => _dbContext.SaveChangesAsync();
}
