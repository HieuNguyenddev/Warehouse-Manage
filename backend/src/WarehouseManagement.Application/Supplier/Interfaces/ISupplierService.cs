// Lớp Application — hợp đồng nghiệp vụ quản lý nhà cung cấp.
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Interfaces;

public interface ISupplierService
{
    // Lấy nhà cung cấp theo Id.
    Task<SupplierResponseDto> GetByIdAsync(int id);

    // Lấy danh sách nhà cung cấp phân trang.
    Task<PagedResult<SupplierResponseDto>> GetAllAsync(PaginationQuery query);

    // Tạo nhà cung cấp mới.
    Task<SupplierResponseDto> CreateAsync(SupplierRequestDto dto);

    // Cập nhật nhà cung cấp.
    Task<SupplierResponseDto> UpdateAsync(int id, SupplierRequestDto dto);

    // Xóa nhà cung cấp theo Id.
    Task DeleteAsync(int id);
}
