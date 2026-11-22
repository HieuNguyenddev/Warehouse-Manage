using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Infrastructure.Persistence;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Infrastructure.Repositories;

/// <summary>
/// Truy vấn và CRUD bảng Units (đơn vị tính).
/// </summary>
public class UnitRepository : IUnitRepository
{
    private readonly ApplicationDbContext _dbContext;

    public UnitRepository(ApplicationDbContext db)
    {
        _dbContext = db;
    }

    /// <summary>Lấy đơn vị theo Id.</summary>
    public Task<Unit?> GetByIdAsync(int id)
        => _dbContext.Units.FirstOrDefaultAsync(x => x.Id == id);

    /// <summary>Phân trang đơn vị; tìm theo Name hoặc Symbol.</summary>
    public async Task<PagedResult<Unit>> GetPagedAsync(string? search, int pageNumber, int pageSize)
    {
        var query = _dbContext.Units.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                x.Name.Contains(search) ||
                x.Symbol.Contains(search));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Unit>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = total
        };
    }

    /// <summary>Kiểm tra tên đơn vị đã tồn tại.</summary>
    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
    {
        var query = _dbContext.Units.AsNoTracking().Where(x => x.Name == name);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    /// <summary>Kiểm tra ký hiệu (Symbol) đã tồn tại.</summary>
    public async Task<bool> ExistsBySymbolAsync(string symbol, int? excludeId = null)
    {
        var query = _dbContext.Units.AsNoTracking().Where(x => x.Symbol == symbol);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    /// <summary>Thêm đơn vị mới.</summary>
    public Task AddAsync(Unit unit) => _dbContext.Units.AddAsync(unit).AsTask();

    /// <summary>Cập nhật đơn vị.</summary>
    public void Update(Unit unit) => _dbContext.Units.Update(unit);

    /// <summary>Xóa đơn vị.</summary>
    public void Delete(Unit unit) => _dbContext.Units.Remove(unit);

    /// <summary>Lưu thay đổi xuống DB.</summary>
    public Task SaveChangesAsync() => _dbContext.SaveChangesAsync();
}
