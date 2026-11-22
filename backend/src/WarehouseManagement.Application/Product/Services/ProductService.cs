using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Services;

// Service xử lý nghiệp vụ sản phẩm (CRUD + quản lý ảnh)
public class ProductService : IProductService
{
    private readonly IProductRepository _repo; // truy cập bảng Products / ProductImages
    private readonly ICategoryRepository _categoryRepo; // dùng để kiểm tra CategoryId có thật không
    private readonly IUnitRepository _unitRepo; // dùng để kiểm tra UnitId có thật không
    private readonly IFileStorageService _files; // lưu / xóa file ảnh (media)

    // Constructor: ASP.NET tự tiêm các dependency khi tạo Service (Dependency Injection)
    public ProductService(
        IProductRepository repo,
        ICategoryRepository categoryRepo,
        IUnitRepository unitRepo,
        IFileStorageService files)
    {
        _repo = repo;
        _categoryRepo = categoryRepo;
        _unitRepo = unitRepo;
        _files = files;
    }

    // Lấy 1 sản phẩm theo Id, trả về DTO cho API
    public async Task<ProductResponseDto> GetByIdAsync(int id)
    {
        var product = await GetProductAsync(id); // lấy entity từ DB, không có thì ném lỗi
        return Map(product); // đổi Entity → DTO rồi trả về
    }

    // Lấy danh sách sản phẩm có phân trang; có thể lọc theo categoryId và search
    public async Task<PagedResult<ProductResponseDto>> GetAllAsync(
        PaginationQuery query,
        int? categoryId = null)
    {
        // Gọi repository lấy 1 trang dữ liệu từ SQL
        var paged = await _repo.GetPagedAsync(
            query.Search?.Trim(), // từ khóa tìm theo tên/SKU (bỏ khoảng trắng 2 đầu)
            categoryId, // null = không lọc danh mục
            query.PageNumber, // số trang (bắt đầu từ 1)
            query.PageSize); // số bản ghi mỗi trang

        // Đóng gói kết quả phân trang, map từng Product → ProductResponseDto
        return new PagedResult<ProductResponseDto>
        {
            Items = paged.Items.Select(Map).ToList(), // danh sách DTO của trang hiện tại
            PageNumber = paged.PageNumber, // giữ nguyên số trang
            PageSize = paged.PageSize, // giữ nguyên kích thước trang
            TotalRecords = paged.TotalRecords // tổng số bản ghi thỏa điều kiện lọc
        };
    }

    // Tạo sản phẩm mới từ dữ liệu client gửi lên (DTO)
    public async Task<ProductResponseDto> CreateAsync(ProductRequestDto dto)
    {
        var sku = dto.Sku.Trim().ToUpperInvariant(); // chuẩn hóa SKU: trim + viết hoa

        await EnsureUniqueSkuAsync(sku); // SKU chưa được dùng bởi sản phẩm khác
        await EnsureRefsAsync(dto.CategoryId, dto.UnitId); // CategoryId và UnitId phải tồn tại

        // Tạo entity Product mới từ DTO
        var product = new Product
        {
            Sku = sku, // mã hàng đã chuẩn hóa
            Name = dto.Name.Trim(), // tên sản phẩm
            Description = dto.Description?.Trim(), // mô tả (có thể null)
            Price = dto.Price, // giá bán
            MinStock = dto.MinStock, // ngưỡng tồn tối thiểu
            CategoryId = dto.CategoryId, // khóa ngoại danh mục
            UnitId = dto.UnitId, // khóa ngoại đơn vị tính
            IsActive = dto.IsActive // đang kinh doanh hay không
        };

        await _repo.AddAsync(product); // đưa entity vào DbContext (chưa ghi SQL)
        await _repo.SaveChangesAsync(); // ghi xuống database, lúc này product.Id mới có giá trị

        // Đọc lại kèm Category/Unit/Images rồi map sang DTO trả về
        return Map(await GetProductAsync(product.Id));
    }

    // Cập nhật sản phẩm theo Id
    public async Task<ProductResponseDto> UpdateAsync(int id, ProductRequestDto dto)
    {
        var product = await GetProductAsync(id); // tìm sản phẩm cần sửa
        var sku = dto.Sku.Trim().ToUpperInvariant(); // chuẩn hóa SKU mới

        await EnsureUniqueSkuAsync(sku, excludeId: id); // cho phép giữ SKU cũ của chính nó
        await EnsureRefsAsync(dto.CategoryId, dto.UnitId); // kiểm tra FK còn hợp lệ

        product.Sku = sku; // gán SKU mới
        product.Name = dto.Name.Trim(); // gán tên mới
        product.Description = dto.Description?.Trim(); // gán mô tả mới
        product.Price = dto.Price; // gán giá mới
        product.MinStock = dto.MinStock; // gán ngưỡng tồn mới
        product.CategoryId = dto.CategoryId; // gán danh mục mới
        product.UnitId = dto.UnitId; // gán đơn vị tính mới
        product.IsActive = dto.IsActive; // gán trạng thái mới
        product.UpdatedAt = DateTime.UtcNow; // đánh dấu thời điểm cập nhật (UTC)

        _repo.Update(product); // đánh dấu entity đã thay đổi
        await _repo.SaveChangesAsync(); // lưu thay đổi xuống DB

        return Map(await GetProductAsync(product.Id)); // trả DTO sau khi cập nhật
    }

    // Xóa sản phẩm và xóa luôn các file ảnh liên quan
    public async Task DeleteAsync(int id)
    {
        var product = await GetProductAsync(id); // tìm sản phẩm cần xóa

        // Lưu URL ảnh trước khi xóa DB (sau khi xóa entity sẽ không còn Images)
        var imageUrls = product.Images.Select(x => x.ImageUrl).ToList();

        _repo.Delete(product); // đánh dấu xóa sản phẩm (+ cascade ảnh tùy cấu hình EF)
        await _repo.SaveChangesAsync(); // thực thi xóa trên SQL

        // Xóa từng file/media trên storage sau khi DB đã xóa thành công
        foreach (var url in imageUrls)
            _files.DeleteFile(url);
    }

    // Upload thêm nhiều ảnh cho 1 sản phẩm
    public async Task<ProductResponseDto> AddImagesAsync(
        int productId,
        IEnumerable<(Stream Stream, string FileName, string? ContentType)> files)
    {
        var product = await GetProductAsync(productId); // sản phẩm nhận ảnh
        var fileList = files.ToList(); // materialize để đếm và duyệt nhiều lần

        if (fileList.Count == 0)
            throw new ValidationException("Chưa chọn ảnh."); // không có file nào

        if (fileList.Count > 10)
            throw new ValidationException("Mỗi lần upload tối đa 10 ảnh."); // giới hạn 1 lần upload

        // SortOrder tiếp theo: nếu chưa có ảnh thì bắt đầu 0, không thì max + 1
        var nextOrder = product.Images.Count == 0
            ? 0
            : product.Images.Max(x => x.SortOrder) + 1;

        var hasPrimary = product.Images.Any(x => x.IsPrimary); // đã có ảnh chính chưa?
        var savedPaths = new List<string>(); // nhớ các path đã lưu để rollback nếu lỗi

        try
        {
            for (var i = 0; i < fileList.Count; i++)
            {
                var (stream, fileName, contentType) = fileList[i]; // tách tuple từng file

                // Lưu file vào storage, nhận về URL/public path
                var path = await _files.SaveImageAsync(stream, fileName, "products", contentType);
                savedPaths.Add(path); // ghi nhận để xóa nếu bước sau lỗi

                // Tạo bản ghi ảnh gắn với sản phẩm
                await _repo.AddImageAsync(new ProductImage
                {
                    ProductId = product.Id, // thuộc sản phẩm nào
                    ImageUrl = path, // đường dẫn/media URL
                    SortOrder = nextOrder + i, // thứ tự hiển thị
                    IsPrimary = !hasPrimary && i == 0 // ảnh đầu tiên làm primary nếu chưa có
                });
            }

            product.UpdatedAt = DateTime.UtcNow; // cập nhật thời gian sửa sản phẩm
            _repo.Update(product); // đánh dấu product đổi
            await _repo.SaveChangesAsync(); // lưu toàn bộ ảnh + product
        }
        catch
        {
            // Có lỗi giữa chừng → xóa các file vừa lưu để tránh rác trên storage
            foreach (var path in savedPaths)
                _files.DeleteFile(path);
            throw; // ném lại lỗi cho middleware/controller xử lý
        }

        return Map(await GetProductAsync(product.Id)); // trả sản phẩm kèm danh sách ảnh mới
    }

    // Thay nội dung 1 ảnh (giữ nguyên slot / IsPrimary / SortOrder)
    public async Task<ProductResponseDto> ReplaceImageAsync(
        int productId,
        int imageId,
        Stream stream,
        string fileName,
        string? contentType = null)
    {
        var product = await GetProductAsync(productId); // kiểm tra sản phẩm tồn tại

        // Tìm đúng ảnh thuộc sản phẩm này
        var image = await _repo.GetImageByIdAsync(productId, imageId)
            ?? throw new NotFoundException("Không tìm thấy ảnh sản phẩm");

        var oldUrl = image.ImageUrl; // giữ URL cũ để xóa sau
        string? newPath = null; // path ảnh mới (null nếu chưa lưu được)

        try
        {
            // Lưu ảnh mới lên storage
            newPath = await _files.SaveImageAsync(stream, fileName, "products", contentType);

            image.ImageUrl = newPath; // trỏ bản ghi sang URL mới
            image.UpdatedAt = DateTime.UtcNow; // thời điểm đổi ảnh
            product.UpdatedAt = DateTime.UtcNow; // thời điểm đổi sản phẩm
            _repo.Update(product); // đánh dấu thay đổi
            await _repo.SaveChangesAsync(); // ghi DB trước khi xóa file cũ
        }
        catch
        {
            // Lưu DB thất bại → xóa ảnh mới vừa upload (nếu có)
            if (newPath is not null)
                _files.DeleteFile(newPath);
            throw;
        }

        // DB đã OK → xóa file cũ (nếu URL khác nhau)
        if (!string.Equals(oldUrl, newPath, StringComparison.OrdinalIgnoreCase))
            _files.DeleteFile(oldUrl);

        return Map(await GetProductAsync(productId));
    }

    // Xóa 1 ảnh của sản phẩm
    public async Task<ProductResponseDto> DeleteImageAsync(int productId, int imageId)
    {
        var product = await GetProductAsync(productId); // kiểm tra sản phẩm

        var image = await _repo.GetImageByIdAsync(productId, imageId)
            ?? throw new NotFoundException("Không tìm thấy ảnh sản phẩm");

        var url = image.ImageUrl; // URL để xóa file sau
        var wasPrimary = image.IsPrimary; // có phải đang xóa ảnh chính không?

        _repo.DeleteImage(image); // xóa bản ghi ảnh
        await _repo.SaveChangesAsync(); // ghi DB
        _files.DeleteFile(url); // xóa file trên storage

        // Nếu vừa xóa ảnh chính → chọn ảnh còn lại (SortOrder nhỏ nhất) làm chính
        if (wasPrimary)
        {
            product = await GetProductAsync(productId); // đọc lại danh sách ảnh còn lại
            var next = product.Images.OrderBy(x => x.SortOrder).FirstOrDefault();
            if (next is not null)
            {
                next.IsPrimary = true; // đặt ảnh kế tiếp làm primary
                next.UpdatedAt = DateTime.UtcNow;
                await _repo.SaveChangesAsync(); // lưu cờ primary mới
            }
        }

        product = await GetProductAsync(productId); // đọc lại sản phẩm sau khi đổi ảnh
        product.UpdatedAt = DateTime.UtcNow; // cập nhật mốc thời gian
        _repo.Update(product);
        await _repo.SaveChangesAsync();

        return Map(await GetProductAsync(productId));
    }

    // Đặt 1 ảnh làm ảnh chính (chỉ 1 ảnh IsPrimary = true)
    public async Task<ProductResponseDto> SetPrimaryImageAsync(int productId, int imageId)
    {
        var product = await GetProductAsync(productId); // lấy sản phẩm kèm Images

        // Đảm bảo imageId thuộc sản phẩm này
        _ = product.Images.FirstOrDefault(x => x.Id == imageId)
            ?? throw new NotFoundException("Không tìm thấy ảnh sản phẩm");

        // Duyệt tất cả ảnh: đúng imageId → true, còn lại → false
        foreach (var img in product.Images)
            img.IsPrimary = img.Id == imageId;

        product.UpdatedAt = DateTime.UtcNow;
        _repo.Update(product);
        await _repo.SaveChangesAsync();

        return Map(await GetProductAsync(productId));
    }

    // Kiểm tra SKU chưa tồn tại; excludeId dùng khi Update để bỏ qua chính sản phẩm đang sửa
    private async Task EnsureUniqueSkuAsync(string sku, int? excludeId = null)
    {
        if (await _repo.ExistsBySkuAsync(sku, excludeId))
            throw new ConflictException("SKU đã tồn tại"); // 409 Conflict
    }

    // Kiểm tra CategoryId và UnitId có tồn tại trong DB không
    private async Task EnsureRefsAsync(int categoryId, int unitId)
    {
        if (await _categoryRepo.GetByIdAsync(categoryId) is null)
            throw new ValidationException("CategoryId không tồn tại."); // 400

        if (await _unitRepo.GetByIdAsync(unitId) is null)
            throw new ValidationException("UnitId không tồn tại."); // 400
    }

    // Lấy Product theo Id; Id không hợp lệ / không tìm thấy → ném exception tương ứng
    private async Task<Product> GetProductAsync(int id)
    {
        if (id <= 0)
            throw new ValidationException("Id không hợp lệ.");

        var product = await _repo.GetByIdAsync(id); // query DB (kèm Include Category/Unit/Images)
        if (product is null)
            throw new NotFoundException("Không tìm thấy sản phẩm"); // 404

        return product;
    }

    // Đổi Entity Product → ProductResponseDto để trả ra API (tránh trả entity thô)
    private static ProductResponseDto Map(Product p) => new()
    {
        Id = p.Id, // khóa chính
        Sku = p.Sku, // mã hàng
        Name = p.Name, // tên
        Description = p.Description, // mô tả
        Price = p.Price, // giá
        MinStock = p.MinStock, // ngưỡng tồn
        IsActive = p.IsActive, // trạng thái
        CategoryId = p.CategoryId, // FK danh mục
        CategoryName = p.Category?.Name, // tên danh mục (từ navigation, có thể null)
        UnitId = p.UnitId, // FK đơn vị
        UnitName = p.Unit?.Name, // tên đơn vị
        UnitSymbol = p.Unit?.Symbol, // ký hiệu đơn vị (kg, hộp...)
        Images = p.Images
            .OrderByDescending(x => x.IsPrimary) // ảnh chính lên trước
            .ThenBy(x => x.SortOrder) // rồi theo thứ tự SortOrder
            .Select(x => new ProductImageDto
            {
                Id = x.Id, // id ảnh
                ImageUrl = x.ImageUrl, // URL xem ảnh
                SortOrder = x.SortOrder, // thứ tự
                IsPrimary = x.IsPrimary // có phải ảnh chính không
            })
            .ToList(),
        CreatedAt = p.CreatedAt, // ngày tạo
        UpdatedAt = p.UpdatedAt // ngày cập nhật gần nhất
    };
}
