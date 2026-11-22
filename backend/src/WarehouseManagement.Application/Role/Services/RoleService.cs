using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Services;

public class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepository;

    public RoleService(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<RoleResponseDto> GetByIdAsync(int id)
    {
        var role = await GetRoleAsync(id);
        return Map(role);
    }

    public async Task<PagedResult<RoleResponseDto>> GetAllAsync(PaginationQuery query)
    {
        var paged = await _roleRepository.GetPagedAsync(
            query.Search?.Trim(),
            query.PageNumber,
            query.PageSize);

        return new PagedResult<RoleResponseDto>
        {
            Items = paged.Items.Select(Map).ToList(),
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalRecords = paged.TotalRecords
        };
    }

    public async Task<RoleResponseDto> CreateAsync(RoleRequestDto dto)
    {
        // B1: chuẩn hóa tên
        var name = dto.Name.Trim();

        // B2: không cho trùng tên
        if (await _roleRepository.ExistsByNameAsync(name))
            throw new ConflictException("Tên vai trò đã tồn tại");

        // B3: tạo entity + lưu
        var role = new Role
        {
            Name = name,
            Description = dto.Description?.Trim()
        };

        await _roleRepository.AddAsync(role);
        await _roleRepository.SaveChangesAsync();
        return Map(role);
    }

    public async Task<RoleResponseDto> UpdateAsync(int id, RoleRequestDto dto)
    {
        var role = await GetRoleAsync(id);
        var name = dto.Name.Trim();

        // Khi sửa: vẫn check trùng, nhưng bỏ qua chính id đang sửa
        if (await _roleRepository.ExistsByNameAsync(name, id))
            throw new ConflictException("Tên vai trò đã tồn tại");

        role.Name = name;
        role.Description = dto.Description?.Trim();
        role.UpdatedAt = DateTime.UtcNow;

        _roleRepository.Update(role);
        await _roleRepository.SaveChangesAsync();
        return Map(role);
    }

    public async Task DeleteAsync(int id)
    {
        var role = await GetRoleAsync(id);
        _roleRepository.Delete(role);
        await _roleRepository.SaveChangesAsync();
    }

    private async Task<Role> GetRoleAsync(int id)
    {
        if (id <= 0)
            throw new ValidationException("Id không hợp lệ.");

        var role = await _roleRepository.GetByIdAsync(id);
        if (role is null)
            throw new NotFoundException("Không tìm thấy vai trò");

        return role;
    }

    private static RoleResponseDto Map(Role role) => new()
    {
        Id = role.Id,
        Name = role.Name,
        Description = role.Description,
        CreatedAt = role.CreatedAt,
        UpdatedAt = role.UpdatedAt
    };
}
