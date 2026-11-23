using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Infrastructure.Persistence;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    // DbContext = "cầu nối" C# ↔ SQL Server
    private readonly ApplicationDbContext _db;

    public ProductRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>Lấy 1 sản phẩm + kèm Category, Unit, Images (Include = JOIN).</summary>
    public Task<Product?> GetByIdAsync(int id)
        => _db.Products
            .Include(x => x.Category)
            .Include(x => x.Unit)
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id);

    /// <summary>
    /// Phân trang danh sách sản phẩm.
    /// Công thức Skip/Take: trang 1 → Skip(0), trang 2 → Skip(pageSize)...
    /// </summary>
    public async Task<PagedResult<Product>> GetPagedAsync(
        string? search,
        int? categoryId,
        int pageNumber,
        int pageSize)
    {
        // B1: tạo query cơ bản (AsNoTracking = chỉ đọc, nhanh hơn)
        var query = _db.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Unit)
            .Include(x => x.Images)
            .AsQueryable();

        // B2: lọc theo từ khóa (tên hoặc SKU)
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Name.Contains(search) || x.Sku.Contains(search));

        // B3: lọc theo danh mục (nếu có)
        if (categoryId is > 0)
            query = query.Where(x => x.CategoryId == categoryId.Value);

        // B4: đếm tổng + lấy đúng 1 trang
        var total = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Product>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = total
        };
    }

    /// <summary>
    /// Catalog nhà thuốc: IsActive, SKU SP####, mỗi tên chỉ 1 SP (Id nhỏ nhất),
    /// sắp theo tên để trang đầu không toàn cùng một nhóm kháng sinh.
    /// </summary>
    public async Task<PagedResult<Product>> GetStorePagedAsync(
        string? search,
        int? categoryId,
        int pageNumber,
        int pageSize)
    {
        var baseQuery = _db.Products
            .AsNoTracking()
            .Where(x => x.IsActive)
            // SKU chuẩn: SP + đúng 4 chữ số (loại SP-001 / mã lệch)
            .Where(x => EF.Functions.Like(x.Sku, "SP[0-9][0-9][0-9][0-9]"));

        if (!string.IsNullOrWhiteSpace(search))
            baseQuery = baseQuery.Where(x => x.Name.Contains(search) || x.Sku.Contains(search));

        if (categoryId is > 0)
            baseQuery = baseQuery.Where(x => x.CategoryId == categoryId.Value);

        // Mỗi tên chỉ giữ 1 sản phẩm (Id nhỏ nhất) — tránh trùng hiển thị
        var uniqueIds = await baseQuery
            .GroupBy(x => x.Name)
            .Select(g => g.Min(x => x.Id))
            .ToListAsync();

        var total = uniqueIds.Count;

        // Load đủ entity rồi xen nhóm + phân trang (catalog ~500, ổn định)
        var all = await _db.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Unit)
            .Include(x => x.Images)
            .Where(x => uniqueIds.Contains(x.Id))
            .ToListAsync();

        // Xen kẽ theo CategoryId để trang đầu đa dạng nhóm thuốc
        var interleaved = InterleaveByCategory(all);
        var items = interleaved
            .Skip((Math.Max(1, pageNumber) - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Product>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = total
        };
    }

    /// <summary>Round-robin theo CategoryId — trang đầu có nhiều nhóm khác nhau.</summary>
    private static List<Product> InterleaveByCategory(List<Product> source)
    {
        var groups = source
            .GroupBy(x => x.CategoryId)
            .OrderBy(g => g.Key)
            .Select(g => new Queue<Product>(g.OrderBy(x => x.Name).ThenBy(x => x.Sku)))
            .ToList();

        var result = new List<Product>(source.Count);
        var added = true;
        while (added)
        {
            added = false;
            foreach (var q in groups)
            {
                if (q.Count == 0) continue;
                result.Add(q.Dequeue());
                added = true;
            }
        }

        return result;
    }

    /// <summary>Kiểm tra SKU đã tồn tại chưa (phục vụ Validate ở Service).</summary>
    public async Task<bool> ExistsBySkuAsync(string sku, int? excludeId = null)
    {
        var query = _db.Products.AsNoTracking().Where(x => x.Sku == sku);

        // Khi Update: bỏ qua chính sản phẩm đang sửa
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);

        return await query.AnyAsync();
    }

    public Task AddAsync(Product product) => _db.Products.AddAsync(product).AsTask();

    public void Update(Product product) => _db.Products.Update(product);

    public void Delete(Product product) => _db.Products.Remove(product);

    public Task AddImageAsync(ProductImage image) => _db.ProductImages.AddAsync(image).AsTask();

    public Task<ProductImage?> GetImageByIdAsync(int productId, int imageId)
        => _db.ProductImages.FirstOrDefaultAsync(x => x.Id == imageId && x.ProductId == productId);

    public void DeleteImage(ProductImage image) => _db.ProductImages.Remove(image);

    /// <summary>Bắt buộc gọi SaveChangesAsync thì SQL mới thực sự ghi xuống DB.</summary>
    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
