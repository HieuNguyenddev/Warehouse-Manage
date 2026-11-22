// Lớp Application — hợp đồng truy cập dữ liệu sản phẩm và ảnh sản phẩm.
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Interfaces;

public interface IProductRepository
{
    // Lấy Product theo Id (kèm ảnh, category, unit...).
    Task<Product?> GetByIdAsync(int id);

    // Lấy danh sách Product phân trang; lọc theo search và categoryId.
    Task<PagedResult<Product>> GetPagedAsync(string? search, int? categoryId, int pageNumber, int pageSize);

    // Kiểm tra SKU đã tồn tại (có thể loại trừ Id khi cập nhật).
    Task<bool> ExistsBySkuAsync(string sku, int? excludeId = null);

    // Thêm Product mới.
    Task AddAsync(Product product);

    // Đánh dấu Product cần cập nhật.
    void Update(Product product);

    // Đánh dấu Product cần xóa.
    void Delete(Product product);

    // Thêm bản ghi ảnh sản phẩm.
    Task AddImageAsync(ProductImage image);

    // Lấy ảnh theo productId và imageId.
    Task<ProductImage?> GetImageByIdAsync(int productId, int imageId);

    // Đánh dấu ảnh cần xóa.
    void DeleteImage(ProductImage image);

    // Lưu thay đổi xuống DB.
    Task SaveChangesAsync();
}
