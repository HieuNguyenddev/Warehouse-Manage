// Lớp Application — dịch vụ nghiệp vụ quản lý danh mục sản phẩm.
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    // Lấy danh mục theo Id.
    public async Task<CategoryResponseDto> GetByIdAsync(int id)
    {
        var category = await GetCategoryAsync(id);
        return ToResponseDto(category);
    }

    // Lấy danh sách danh mục phân trang.
    public async Task<PagedResult<CategoryResponseDto>> GetAllAsync(PaginationQuery query)
    {
        var pagedResult = await _categoryRepository.GetPagedAsync(query.Search?.Trim(), query.PageNumber, query.PageSize);

        return new PagedResult<CategoryResponseDto>
        {
            Items = pagedResult.Items.Select(ToResponseDto).ToList(),
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize,
            TotalRecords = pagedResult.TotalRecords
        };
    }

    // Tạo danh mục mới (tên không trùng).
    public async Task<CategoryResponseDto> CreateAsync(CategoryRequestDto dto)
    {
        var name = dto.Name.Trim();

        if (await _categoryRepository.ExistsByNameAsync(name))
            throw new ConflictException("Tên danh mục đã tồn tại");

        var category = new Category
        {
            Name = name,
            Description = dto.Description?.Trim(),
            IsActive = dto.IsActive
        };

        await _categoryRepository.AddAsync(category);
        await _categoryRepository.SaveChangesAsync();
        return ToResponseDto(category);
    }

    // Cập nhật danh mục.
    public async Task<CategoryResponseDto> UpdateAsync(int id, CategoryRequestDto dto)
    {
        var category = await GetCategoryAsync(id);
        var name = dto.Name.Trim();

        if (await _categoryRepository.ExistsByNameAsync(name, id))
            throw new ConflictException("Tên danh mục đã tồn tại");

        category.Name = name;
        category.Description = dto.Description?.Trim();
        category.IsActive = dto.IsActive;
        category.UpdatedAt = DateTime.UtcNow;

        _categoryRepository.Update(category);
        await _categoryRepository.SaveChangesAsync();
        return ToResponseDto(category);
    }

    // Xóa danh mục theo Id.
    public async Task DeleteAsync(int id)
    {
        var category = await GetCategoryAsync(id);
        _categoryRepository.Delete(category);
        await _categoryRepository.SaveChangesAsync();
    }

    // Lấy entity Category hoặc ném lỗi.
    private async Task<Category> GetCategoryAsync(int id)
    {
        if (id <= 0)
            throw new ValidationException("Id không hợp lệ.");

        var category = await _categoryRepository.GetByIdAsync(id);
        if (category is null)
            throw new NotFoundException("Không tìm thấy danh mục");

        return category;
    }

    // Map entity → DTO phản hồi.
    private static CategoryResponseDto ToResponseDto(Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Description = category.Description,
        IsActive = category.IsActive,
        CreatedAt = category.CreatedAt,
        UpdatedAt = category.UpdatedAt
    };
}
