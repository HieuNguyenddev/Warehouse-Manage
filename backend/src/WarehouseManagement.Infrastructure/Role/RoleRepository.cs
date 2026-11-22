using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Infrastructure.Persistence;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Infrastructure.Repositories;

/// <summary>
/// Truy vấn và CRUD bảng Roles.
/// </summary>
public class RoleRepository : IRoleRepository
{
    private readonly ApplicationDbContext _dbContext;

    public RoleRepository(ApplicationDbContext db)
    {
        _dbContext = db;
    }

    /// <summary>Lấy role theo Id.</summary>
    public Task<Role?> GetByIdAsync(int id)
        => _dbContext.Roles.FirstOrDefaultAsync(x => x.Id == id);

    /// <summary>Lấy role theo tên (Admin, Staff...).</summary>
    public Task<Role?> GetByNameAsync(string name)
        => _dbContext.Roles.FirstOrDefaultAsync(x => x.Name == name);

    /// <summary>Phân trang role; lọc theo tên nếu có search.</summary>
    public async Task<PagedResult<Role>> GetPagedAsync(string? search, int pageNumber, int pageSize)
    {
        var query = _dbContext.Roles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Name.Contains(search));

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Role>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = total
        };
    }

    /// <summary>Kiểm tra tên role đã tồn tại (có thể loại trừ 1 Id khi sửa).</summary>
    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
    {
        var query = _dbContext.Roles.AsNoTracking().Where(x => x.Name == name);

        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);

        return await query.AnyAsync();
    }

    /// <summary>Thêm role mới (chưa SaveChanges).</summary>
    public Task AddAsync(Role role) => _dbContext.Roles.AddAsync(role).AsTask();

    /// <summary>Đánh dấu role đã thay đổi.</summary>
    public void Update(Role role) => _dbContext.Roles.Update(role);

    /// <summary>Xóa role khỏi DbContext.</summary>
    public void Delete(Role role) => _dbContext.Roles.Remove(role);

    /// <summary>Lưu thay đổi xuống DB.</summary>
    public Task SaveChangesAsync() => _dbContext.SaveChangesAsync();
}
