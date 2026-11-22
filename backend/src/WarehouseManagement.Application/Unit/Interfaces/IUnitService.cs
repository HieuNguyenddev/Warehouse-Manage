// Lớp Application — hợp đồng nghiệp vụ quản lý đơn vị tính.
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Interfaces;

public interface IUnitService
{
    // Lấy đơn vị tính theo Id.
    Task<UnitResponseDto> GetByIdAsync(int id);

    // Lấy danh sách đơn vị tính phân trang.
    Task<PagedResult<UnitResponseDto>> GetAllAsync(PaginationQuery query);

    // Tạo đơn vị tính mới.
    Task<UnitResponseDto> CreateAsync(UnitRequestDto dto);

    // Cập nhật đơn vị tính.
    Task<UnitResponseDto> UpdateAsync(int id, UnitRequestDto dto);

    // Xóa đơn vị tính theo Id.
    Task DeleteAsync(int id);
}
