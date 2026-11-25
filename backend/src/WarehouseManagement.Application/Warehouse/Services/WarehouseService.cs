// Lớp Application — dịch vụ nghiệp vụ quản lý kho hàng.
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Services;

public class WarehouseService : IWarehouseService
{
    private readonly IWarehouseRepository _warehouseRepository;

    public WarehouseService(IWarehouseRepository warehouseRepository)
    {
        _warehouseRepository = warehouseRepository;
    }

    // Lấy kho theo Id.
    public async Task<WarehouseResponseDto> GetByIdAsync(int id)
        => ToResponseDto(await GetWarehouseAsync(id));

    // Lấy danh sách kho phân trang.
    public async Task<PagedResult<WarehouseResponseDto>> GetAllAsync(PaginationQuery query)
    {
        var pagedResult = await _warehouseRepository.GetPagedAsync(query.Search?.Trim(), query.PageNumber, query.PageSize);

        return new PagedResult<WarehouseResponseDto>
        {
            Items = pagedResult.Items.Select(ToResponseDto).ToList(),
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize,
            TotalRecords = pagedResult.TotalRecords
        };
    }

    // Tạo kho mới (mã chuẩn hóa uppercase; mã/tên không trùng).
    public async Task<WarehouseResponseDto> CreateAsync(WarehouseRequestDto dto)
    {
        var code = dto.Code.Trim().ToUpperInvariant();
        var name = dto.Name.Trim();

        if (await _warehouseRepository.ExistsByCodeAsync(code))
            throw new ConflictException("Mã kho đã tồn tại");

        if (await _warehouseRepository.ExistsByNameAsync(name))
            throw new ConflictException("Tên kho đã tồn tại");

        var warehouse = new Warehouse
        {
            Code = code,
            Name = name,
            Address = dto.Address?.Trim(),
            Phone = dto.Phone?.Trim(),
            ManagerName = dto.ManagerName?.Trim(),
            Note = dto.Note?.Trim(),
            IsActive = dto.IsActive,
            WarehouseType = dto.WarehouseType
        };

        await _warehouseRepository.AddAsync(warehouse);
        await _warehouseRepository.SaveChangesAsync();
        return ToResponseDto(warehouse);
    }

    // Cập nhật thông tin kho.
    public async Task<WarehouseResponseDto> UpdateAsync(int id, WarehouseRequestDto dto)
    {
        var warehouse = await GetWarehouseAsync(id);
        var code = dto.Code.Trim().ToUpperInvariant();
        var name = dto.Name.Trim();

        if (await _warehouseRepository.ExistsByCodeAsync(code, id))
            throw new ConflictException("Mã kho đã tồn tại");

        if (await _warehouseRepository.ExistsByNameAsync(name, id))
            throw new ConflictException("Tên kho đã tồn tại");

        warehouse.Code = code;
        warehouse.Name = name;
        warehouse.Address = dto.Address?.Trim();
        warehouse.Phone = dto.Phone?.Trim();
        warehouse.ManagerName = dto.ManagerName?.Trim();
        warehouse.Note = dto.Note?.Trim();
        warehouse.IsActive = dto.IsActive;
        warehouse.WarehouseType = dto.WarehouseType;
        warehouse.UpdatedAt = DateTime.UtcNow;

        _warehouseRepository.Update(warehouse);
        await _warehouseRepository.SaveChangesAsync();
        return ToResponseDto(warehouse);
    }

    // Xóa kho theo Id.
    public async Task DeleteAsync(int id)
    {
        var warehouse = await GetWarehouseAsync(id);
        _warehouseRepository.Delete(warehouse);
        await _warehouseRepository.SaveChangesAsync();
    }

    // Lấy entity Warehouse hoặc ném lỗi.
    private async Task<Warehouse> GetWarehouseAsync(int id)
    {
        if (id <= 0)
            throw new ValidationException("Id không hợp lệ.");

        var warehouse = await _warehouseRepository.GetByIdAsync(id);
        if (warehouse is null)
            throw new NotFoundException("Không tìm thấy kho hàng");

        return warehouse;
    }

    // Map entity → DTO phản hồi.
    private static WarehouseResponseDto ToResponseDto(Warehouse warehouse) => new()
    {
        Id = warehouse.Id,
        Code = warehouse.Code,
        Name = warehouse.Name,
        Address = warehouse.Address,
        Phone = warehouse.Phone,
        ManagerName = warehouse.ManagerName,
        Note = warehouse.Note,
        IsActive = warehouse.IsActive,
        WarehouseType = warehouse.WarehouseType.ToString(),
        CreatedAt = warehouse.CreatedAt,
        UpdatedAt = warehouse.UpdatedAt
    };
}
