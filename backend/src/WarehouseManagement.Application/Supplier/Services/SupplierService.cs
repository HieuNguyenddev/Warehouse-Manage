// Lớp Application — dịch vụ nghiệp vụ quản lý nhà cung cấp.
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Services;

public class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _supplierRepository;

    public SupplierService(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    // Lấy nhà cung cấp theo Id.
    public async Task<SupplierResponseDto> GetByIdAsync(int id)
    {
        var supplier = await GetSupplierAsync(id);
        return ToResponseDto(supplier);
    }

    // Lấy danh sách nhà cung cấp phân trang.
    public async Task<PagedResult<SupplierResponseDto>> GetAllAsync(PaginationQuery query)
    {
        var pagedResult = await _supplierRepository.GetPagedAsync(query.Search?.Trim(), query.PageNumber, query.PageSize);

        return new PagedResult<SupplierResponseDto>
        {
            Items = pagedResult.Items.Select(ToResponseDto).ToList(),
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize,
            TotalRecords = pagedResult.TotalRecords
        };
    }

    // Tạo nhà cung cấp (kiểm tra trùng tên / email / MST).
    public async Task<SupplierResponseDto> CreateAsync(SupplierRequestDto dto)
    {
        var name = dto.Name.Trim();
        var email = dto.Email?.Trim();
        var taxCode = dto.TaxCode?.Trim();

        if (await _supplierRepository.ExistsByNameAsync(name))
            throw new ConflictException("Tên nhà cung cấp đã tồn tại");

        if (!string.IsNullOrEmpty(email) && await _supplierRepository.ExistsByEmailAsync(email))
            throw new ConflictException("Email nhà cung cấp đã tồn tại");

        if (!string.IsNullOrEmpty(taxCode) && await _supplierRepository.ExistsByTaxCodeAsync(taxCode))
            throw new ConflictException("Mã số thuế đã tồn tại");

        var supplier = new Supplier
        {
            Name = name,
            ContactName = dto.ContactName?.Trim(),
            Phone = dto.Phone?.Trim(),
            Email = email,
            Address = dto.Address?.Trim(),
            TaxCode = taxCode,
            Note = dto.Note?.Trim(),
            IsActive = dto.IsActive
        };

        await _supplierRepository.AddAsync(supplier);
        await _supplierRepository.SaveChangesAsync();
        return ToResponseDto(supplier);
    }

    // Cập nhật nhà cung cấp.
    public async Task<SupplierResponseDto> UpdateAsync(int id, SupplierRequestDto dto)
    {
        var supplier = await GetSupplierAsync(id);
        var name = dto.Name.Trim();
        var email = dto.Email?.Trim();
        var taxCode = dto.TaxCode?.Trim();

        if (await _supplierRepository.ExistsByNameAsync(name, id))
            throw new ConflictException("Tên nhà cung cấp đã tồn tại");

        if (!string.IsNullOrEmpty(email) && await _supplierRepository.ExistsByEmailAsync(email, id))
            throw new ConflictException("Email nhà cung cấp đã tồn tại");

        if (!string.IsNullOrEmpty(taxCode) && await _supplierRepository.ExistsByTaxCodeAsync(taxCode, id))
            throw new ConflictException("Mã số thuế đã tồn tại");

        supplier.Name = name;
        supplier.ContactName = dto.ContactName?.Trim();
        supplier.Phone = dto.Phone?.Trim();
        supplier.Email = email;
        supplier.Address = dto.Address?.Trim();
        supplier.TaxCode = taxCode;
        supplier.Note = dto.Note?.Trim();
        supplier.IsActive = dto.IsActive;
        supplier.UpdatedAt = DateTime.UtcNow;

        _supplierRepository.Update(supplier);
        await _supplierRepository.SaveChangesAsync();
        return ToResponseDto(supplier);
    }

    // Xóa nhà cung cấp theo Id.
    public async Task DeleteAsync(int id)
    {
        var supplier = await GetSupplierAsync(id);
        _supplierRepository.Delete(supplier);
        await _supplierRepository.SaveChangesAsync();
    }

    // Lấy entity Supplier hoặc ném lỗi.
    private async Task<Supplier> GetSupplierAsync(int id)
    {
        if (id <= 0)
            throw new ValidationException("Id không hợp lệ.");

        var supplier = await _supplierRepository.GetByIdAsync(id);
        if (supplier is null)
            throw new NotFoundException("Không tìm thấy nhà cung cấp");

        return supplier;
    }

    // Map entity → DTO phản hồi.
    private static SupplierResponseDto ToResponseDto(Supplier supplier) => new()
    {
        Id = supplier.Id,
        Name = supplier.Name,
        ContactName = supplier.ContactName,
        Phone = supplier.Phone,
        Email = supplier.Email,
        Address = supplier.Address,
        TaxCode = supplier.TaxCode,
        Note = supplier.Note,
        IsActive = supplier.IsActive,
        CreatedAt = supplier.CreatedAt,
        UpdatedAt = supplier.UpdatedAt
    };
}
