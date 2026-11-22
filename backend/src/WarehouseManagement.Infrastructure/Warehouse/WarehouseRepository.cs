using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Infrastructure.Persistence;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Infrastructure.Repositories;

/// <summary>
/// Truy vấn và CRUD bảng Warehouses (kho).
/// </summary>
public class WarehouseRepository : IWarehouseRepository
{
    private readonly ApplicationDbContext _dbContext;

    public WarehouseRepository(ApplicationDbContext db)
    {
        _dbContext = db;
    }

    /// <summary>Lấy kho theo Id.</summary>
    public Task<Warehouse?> GetByIdAsync(int id)
        => _dbContext.Warehouses.FirstOrDefaultAsync(x => x.Id == id);

    /// <summary>Phân trang kho; tìm theo Code, Name, Address, Phone, ManagerName.</summary>
    public async Task<PagedResult<Warehouse>> GetPagedAsync(string? search, int pageNumber, int pageSize)
    {
        var query = _dbContext.Warehouses.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                x.Code.Contains(search) ||
                x.Name.Contains(search) ||
                (x.Address != null && x.Address.Contains(search)) ||
                (x.Phone != null && x.Phone.Contains(search)) ||
                (x.ManagerName != null && x.ManagerName.Contains(search)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Warehouse>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = total
        };
    }

    /// <summary>Kiểm tra mã kho đã tồn tại.</summary>
    public async Task<bool> ExistsByCodeAsync(string code, int? excludeId = null)
    {
        var query = _dbContext.Warehouses.AsNoTracking().Where(x => x.Code == code);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    /// <summary>Kiểm tra tên kho đã tồn tại.</summary>
    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
    {
        var query = _dbContext.Warehouses.AsNoTracking().Where(x => x.Name == name);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    /// <summary>Thêm kho mới.</summary>
    public Task AddAsync(Warehouse warehouse) => _dbContext.Warehouses.AddAsync(warehouse).AsTask();

    /// <summary>Cập nhật kho.</summary>
    public void Update(Warehouse warehouse) => _dbContext.Warehouses.Update(warehouse);

    /// <summary>Xóa kho.</summary>
    public void Delete(Warehouse warehouse) => _dbContext.Warehouses.Remove(warehouse);

    /// <summary>Lưu thay đổi xuống DB.</summary>
    public Task SaveChangesAsync() => _dbContext.SaveChangesAsync();
}
