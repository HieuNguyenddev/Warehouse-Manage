// Lớp Application — hợp đồng nghiệp vụ quản lý danh mục sản phẩm.
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Interfaces;

public interface ICategoryService
{
    // Lấy danh mục theo Id.
    Task<CategoryResponseDto> GetByIdAsync(int id);

    // Lấy danh sách danh mục phân trang.
    Task<PagedResult<CategoryResponseDto>> GetAllAsync(PaginationQuery query);

    // Tạo danh mục mới.
    Task<CategoryResponseDto> CreateAsync(CategoryRequestDto dto);

    // Cập nhật danh mục.
    Task<CategoryResponseDto> UpdateAsync(int id, CategoryRequestDto dto);

    // Xóa danh mục theo Id.
    Task DeleteAsync(int id);
}
