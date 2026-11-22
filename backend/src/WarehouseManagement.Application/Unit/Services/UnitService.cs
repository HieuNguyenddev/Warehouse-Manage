// Lớp Application — dịch vụ nghiệp vụ quản lý đơn vị tính.
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Services;

public class UnitService : IUnitService
{
    private readonly IUnitRepository _unitRepository;

    public UnitService(IUnitRepository unitRepository)
    {
        _unitRepository = unitRepository;
    }

    // Lấy đơn vị tính theo Id.
    public async Task<UnitResponseDto> GetByIdAsync(int id)
    {
        var unit = await GetUnitAsync(id);
        return ToResponseDto(unit);
    }

    // Lấy danh sách đơn vị tính phân trang.
    public async Task<PagedResult<UnitResponseDto>> GetAllAsync(PaginationQuery query)
    {
        var pagedResult = await _unitRepository.GetPagedAsync(query.Search?.Trim(), query.PageNumber, query.PageSize);

        return new PagedResult<UnitResponseDto>
        {
            Items = pagedResult.Items.Select(ToResponseDto).ToList(),
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize,
            TotalRecords = pagedResult.TotalRecords
        };
    }

    // Tạo đơn vị mới (tên và ký hiệu không trùng).
    public async Task<UnitResponseDto> CreateAsync(UnitRequestDto dto)
    {
        var name = dto.Name.Trim();
        var symbol = dto.Symbol.Trim();

        if (await _unitRepository.ExistsByNameAsync(name))
            throw new ConflictException("Tên đơn vị đã tồn tại");

        if (await _unitRepository.ExistsBySymbolAsync(symbol))
            throw new ConflictException("Ký hiệu đơn vị đã tồn tại");

        var unit = new Unit
        {
            Name = name,
            Symbol = symbol,
            Description = dto.Description?.Trim(),
            IsActive = dto.IsActive
        };

        await _unitRepository.AddAsync(unit);
        await _unitRepository.SaveChangesAsync();
        return ToResponseDto(unit);
    }

    // Cập nhật đơn vị tính.
    public async Task<UnitResponseDto> UpdateAsync(int id, UnitRequestDto dto)
    {
        var unit = await GetUnitAsync(id);
        var name = dto.Name.Trim();
        var symbol = dto.Symbol.Trim();

        if (await _unitRepository.ExistsByNameAsync(name, id))
            throw new ConflictException("Tên đơn vị đã tồn tại");

        if (await _unitRepository.ExistsBySymbolAsync(symbol, id))
            throw new ConflictException("Ký hiệu đơn vị đã tồn tại");

        unit.Name = name;
        unit.Symbol = symbol;
        unit.Description = dto.Description?.Trim();
        unit.IsActive = dto.IsActive;
        unit.UpdatedAt = DateTime.UtcNow;

        _unitRepository.Update(unit);
        await _unitRepository.SaveChangesAsync();
        return ToResponseDto(unit);
    }

    // Xóa đơn vị tính theo Id.
    public async Task DeleteAsync(int id)
    {
        var unit = await GetUnitAsync(id);
        _unitRepository.Delete(unit);
        await _unitRepository.SaveChangesAsync();
    }

    // Lấy entity Unit hoặc ném lỗi.
    private async Task<Unit> GetUnitAsync(int id)
    {
        if (id <= 0)
            throw new ValidationException("Id không hợp lệ.");

        var unit = await _unitRepository.GetByIdAsync(id);
        if (unit is null)
            throw new NotFoundException("Không tìm thấy đơn vị tính");

        return unit;
    }

    // Map entity → DTO phản hồi.
    private static UnitResponseDto ToResponseDto(Unit unit) => new()
    {
        Id = unit.Id,
        Name = unit.Name,
        Symbol = unit.Symbol,
        Description = unit.Description,
        IsActive = unit.IsActive,
        CreatedAt = unit.CreatedAt,
        UpdatedAt = unit.UpdatedAt
    };
}
