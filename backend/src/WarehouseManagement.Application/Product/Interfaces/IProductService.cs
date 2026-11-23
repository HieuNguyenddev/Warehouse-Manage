// Lớp Application — hợp đồng nghiệp vụ quản lý sản phẩm và ảnh sản phẩm.
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Interfaces;

public interface IProductService
{
    // Lấy sản phẩm theo Id (kèm ảnh).
    Task<ProductResponseDto> GetByIdAsync(int id);

    // Lấy danh sách sản phẩm phân trang; có thể lọc theo categoryId.
    Task<PagedResult<ProductResponseDto>> GetAllAsync(PaginationQuery query, int? categoryId = null);

    /// <summary>Catalog cổng nhà thuốc (không trùng tên, SKU chuẩn).</summary>
    Task<PagedResult<ProductResponseDto>> GetStoreCatalogAsync(PaginationQuery query, int? categoryId = null);

    // Tạo sản phẩm mới.
    Task<ProductResponseDto> CreateAsync(ProductRequestDto dto);

    // Cập nhật sản phẩm.
    Task<ProductResponseDto> UpdateAsync(int id, ProductRequestDto dto);

    // Xóa sản phẩm theo Id.
    Task DeleteAsync(int id);

    // Thêm nhiều ảnh vào sản phẩm.
    Task<ProductResponseDto> AddImagesAsync(
        int productId,
        IEnumerable<(Stream Stream, string FileName, string? ContentType)> files);

    // Thay file ảnh tại slot hiện có (giữ IsPrimary / SortOrder).
    Task<ProductResponseDto> ReplaceImageAsync(
        int productId,
        int imageId,
        Stream stream,
        string fileName,
        string? contentType = null);

    // Xóa một ảnh của sản phẩm.
    Task<ProductResponseDto> DeleteImageAsync(int productId, int imageId);

    // Đặt ảnh làm ảnh chính (primary).
    Task<ProductResponseDto> SetPrimaryImageAsync(int productId, int imageId);
}
