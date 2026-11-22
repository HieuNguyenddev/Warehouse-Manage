using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Infrastructure.Persistence;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Infrastructure.Repositories;

/// <summary>
/// Truy vấn và CRUD bảng Categories (danh mục sản phẩm).
/// </summary>
public class CategoryRepository : ICategoryRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CategoryRepository(ApplicationDbContext db)
    {
        _dbContext = db;
    }

    /// <summary>Lấy danh mục theo Id.</summary>
    public Task<Category?> GetByIdAsync(int id)
        => _dbContext.Categories.FirstOrDefaultAsync(x => x.Id == id);

    /// <summary>Phân trang danh mục; lọc theo tên nếu có search.</summary>
    public async Task<PagedResult<Category>> GetPagedAsync(string? search, int pageNumber, int pageSize)
    {
        var query = _dbContext.Categories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Name.Contains(search));

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Category>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = total
        };
    }

    /// <summary>Kiểm tra tên danh mục đã tồn tại.</summary>
    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
    {
        var query = _dbContext.Categories.AsNoTracking().Where(x => x.Name == name);

        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);

        return await query.AnyAsync();
    }

    /// <summary>Thêm danh mục mới.</summary>
    public Task AddAsync(Category category) => _dbContext.Categories.AddAsync(category).AsTask();

    /// <summary>Cập nhật danh mục.</summary>
    public void Update(Category category) => _dbContext.Categories.Update(category);

    /// <summary>Xóa danh mục.</summary>
    public void Delete(Category category) => _dbContext.Categories.Remove(category);

    /// <summary>Lưu thay đổi xuống DB.</summary>
    public Task SaveChangesAsync() => _dbContext.SaveChangesAsync();
}
