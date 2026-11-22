using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Infrastructure.Persistence;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Infrastructure.Repositories;

/// <summary>
/// Truy vấn và CRUD bảng Users (kèm Role).
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _dbContext;

    public UserRepository(ApplicationDbContext db)
    {
        _dbContext = db;
    }

    /// <summary>Lấy user theo Id, Include Role.</summary>
    public Task<User?> GetByIdAsync(int id)
        => _dbContext.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == id);

    /// <summary>Lấy user theo Username (dùng đăng nhập), Include Role.</summary>
    public Task<User?> GetByUsernameAsync(string username)
        => _dbContext.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Username == username);

    /// <summary>Phân trang user; tìm theo Username / FullName / Email.</summary>
    public async Task<PagedResult<User>> GetPagedAsync(string? search, int pageNumber, int pageSize)
    {
        var query = _dbContext.Users
            .AsNoTracking()
            .Include(x => x.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                x.Username.Contains(search) ||
                x.FullName.Contains(search) ||
                x.Email.Contains(search));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<User>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = total
        };
    }

    /// <summary>Kiểm tra Username đã tồn tại (excludeId khi cập nhật).</summary>
    public async Task<bool> ExistsByUsernameAsync(string username, int? excludeId = null)
    {
        var query = _dbContext.Users.AsNoTracking().Where(x => x.Username == username);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    /// <summary>Kiểm tra Email đã tồn tại (excludeId khi cập nhật).</summary>
    public async Task<bool> ExistsByEmailAsync(string email, int? excludeId = null)
    {
        var query = _dbContext.Users.AsNoTracking().Where(x => x.Email == email);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    /// <summary>Thêm user mới.</summary>
    public Task AddAsync(User user) => _dbContext.Users.AddAsync(user).AsTask();

    /// <summary>Cập nhật user.</summary>
    public void Update(User user) => _dbContext.Users.Update(user);

    /// <summary>Xóa user.</summary>
    public void Delete(User user) => _dbContext.Users.Remove(user);

    /// <summary>Lưu thay đổi xuống DB.</summary>
    public Task SaveChangesAsync() => _dbContext.SaveChangesAsync();
}
