// Lớp Application — dịch vụ nghiệp vụ khách hàng (admin CRUD + tự phục vụ hồ sơ/avatar).
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repo;
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IFileStorageService _files;

    public CustomerService(
        ICustomerRepository repo,
        IUserRepository userRepo,
        IRoleRepository roleRepo,
        IFileStorageService files)
    {
        _repo = repo;
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _files = files;
    }

    // Lấy khách hàng theo Id.
    public async Task<CustomerResponseDto> GetByIdAsync(int id)
    {
        var customer = await GetCustomerAsync(id);
        return Map(customer);
    }

    // Lấy khách hàng theo UserId đăng nhập.
    public async Task<CustomerResponseDto> GetByUserIdAsync(int userId)
    {
        if (userId <= 0)
            throw new ValidationException("UserId không hợp lệ.");

        var customer = await _repo.GetByUserIdAsync(userId);
        if (customer is null)
            throw new NotFoundException("Không tìm thấy hồ sơ khách hàng");

        return Map(customer);
    }

    // Lấy danh sách khách hàng phân trang.
    public async Task<PagedResult<CustomerResponseDto>> GetAllAsync(PaginationQuery query)
    {
        var paged = await _repo.GetPagedAsync(query.Search?.Trim(), query.PageNumber, query.PageSize);

        return new PagedResult<CustomerResponseDto>
        {
            Items = paged.Items.Select(Map).ToList(),
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalRecords = paged.TotalRecords
        };
    }

    // Tạo khách hàng; tùy chọn kèm tài khoản User role Customer.
    public async Task<CustomerResponseDto> CreateAsync(CustomerRequestDto dto)
    {
        var name = dto.Name.Trim();
        var email = dto.Email?.Trim();
        var taxCode = dto.TaxCode?.Trim();

        await EnsureUniqueAsync(name, email, taxCode, excludeId: null);

        var customer = new Customer
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

        // Có Username + Password -> tạo User role Customer
        if (!string.IsNullOrWhiteSpace(dto.Username))
        {
            customer.User = await CreateCustomerUserAsync(
                dto.Username.Trim(),
                dto.Password!,
                email!,
                dto.ContactName?.Trim() ?? name,
                dto.Phone?.Trim(),
                dto.IsActive);
        }

        await _repo.AddAsync(customer);
        await _repo.SaveChangesAsync();

        return Map(await GetCustomerAsync(customer.Id));
    }

    // Cập nhật khách hàng (admin); có thể tạo/sửa tài khoản liên kết.
    public async Task<CustomerResponseDto> UpdateAsync(int id, CustomerRequestDto dto)
    {
        var customer = await GetCustomerAsync(id);
        var name = dto.Name.Trim();
        var email = dto.Email?.Trim();
        var taxCode = dto.TaxCode?.Trim();

        await EnsureUniqueAsync(name, email, taxCode, id);

        customer.Name = name;
        customer.ContactName = dto.ContactName?.Trim();
        customer.Phone = dto.Phone?.Trim();
        customer.Email = email;
        customer.Address = dto.Address?.Trim();
        customer.TaxCode = taxCode;
        customer.Note = dto.Note?.Trim();
        customer.IsActive = dto.IsActive;
        customer.UpdatedAt = DateTime.UtcNow;

        // Chưa có tài khoản + gửi Username/Password -> tạo mới
        if (customer.UserId is null && !string.IsNullOrWhiteSpace(dto.Username))
        {
            customer.User = await CreateCustomerUserAsync(
                dto.Username.Trim(),
                dto.Password!,
                email!,
                dto.ContactName?.Trim() ?? name,
                dto.Phone?.Trim(),
                dto.IsActive);
        }
        // Đã có tài khoản -> cập nhật thông tin + mật khẩu (nếu có)
        else if (customer.User is not null)
        {
            customer.User.FullName = dto.ContactName?.Trim() ?? name;
            customer.User.Email = email ?? customer.User.Email;
            customer.User.Phone = dto.Phone?.Trim();
            customer.User.IsActive = dto.IsActive;
            customer.User.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(dto.Password))
                customer.User.PasswordHash = PasswordHasher.HashPassword(dto.Password);

            // Email user phải unique
            if (await _userRepo.ExistsByEmailAsync(customer.User.Email, customer.User.Id))
                throw new ConflictException("Email đã được dùng bởi tài khoản khác");
        }

        _repo.Update(customer);
        await _repo.SaveChangesAsync();

        return Map(await GetCustomerAsync(customer.Id));
    }

    public async Task DeleteAsync(int id)
    {
        var customer = await GetCustomerAsync(id);
        var avatar = customer.AvatarUrl;
        var user = customer.User;

        _repo.Delete(customer);
        if (user is not null)
            _userRepo.Delete(user);

        await _repo.SaveChangesAsync();
        _files.DeleteFile(avatar);
    }

    public async Task<CustomerResponseDto> UploadAvatarAsync(
        int id,
        Stream fileStream,
        string fileName,
        string? contentType = null)
    {
        var customer = await GetCustomerAsync(id);
        var oldAvatar = customer.AvatarUrl;

        var path = await _files.SaveAvatarAsync(fileStream, fileName, contentType);
        customer.AvatarUrl = path;
        customer.UpdatedAt = DateTime.UtcNow;

        _repo.Update(customer);
        await _repo.SaveChangesAsync();

        if (!string.IsNullOrEmpty(oldAvatar) && oldAvatar != path)
            _files.DeleteFile(oldAvatar);

        return Map(customer);
    }

    public async Task<CustomerResponseDto> UpdateProfileByUserIdAsync(int userId, CustomerProfileUpdateDto dto)
    {
        if (userId <= 0)
            throw new ValidationException("UserId không hợp lệ.");

        var customer = await _repo.GetByUserIdAsync(userId)
            ?? throw new NotFoundException("Không tìm thấy hồ sơ khách hàng");

        if (!customer.IsActive)
            throw new ConflictException("Tài khoản khách hàng đang bị khóa.");

        var name = dto.Name.Trim();
        var email = dto.Email?.Trim();
        var phone = dto.Phone?.Trim();

        await EnsureUniqueAsync(name, email, customer.TaxCode, customer.Id);

        customer.Name = name;
        customer.ContactName = dto.ContactName?.Trim();
        customer.Phone = phone;
        customer.Email = email;
        customer.Address = dto.Address?.Trim();
        customer.Note = dto.Note?.Trim();
        customer.UpdatedAt = DateTime.UtcNow;

        if (customer.User is not null)
        {
            customer.User.FullName = dto.ContactName?.Trim() ?? name;
            if (!string.IsNullOrWhiteSpace(email))
                customer.User.Email = email;
            customer.User.Phone = phone;
            customer.User.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(dto.NewPassword))
                customer.User.PasswordHash = PasswordHasher.HashPassword(dto.NewPassword);

            if (await _userRepo.ExistsByEmailAsync(customer.User.Email, customer.User.Id))
                throw new ConflictException("Email đã được dùng bởi tài khoản khác");
        }

        _repo.Update(customer);
        await _repo.SaveChangesAsync();
        return Map(await GetCustomerAsync(customer.Id));
    }

    public async Task<CustomerResponseDto> UploadAvatarByUserIdAsync(
        int userId,
        Stream fileStream,
        string fileName,
        string? contentType = null)
    {
        if (userId <= 0)
            throw new ValidationException("UserId không hợp lệ.");

        var customer = await _repo.GetByUserIdAsync(userId)
            ?? throw new NotFoundException("Không tìm thấy hồ sơ khách hàng");

        return await UploadAvatarAsync(customer.Id, fileStream, fileName, contentType);
    }

    private async Task EnsureUniqueAsync(string name, string? email, string? taxCode, int? excludeId)
    {
        if (await _repo.ExistsByNameAsync(name, excludeId))
            throw new ConflictException("Tên khách hàng đã tồn tại");

        if (!string.IsNullOrEmpty(email) && await _repo.ExistsByEmailAsync(email, excludeId))
            throw new ConflictException("Email khách hàng đã tồn tại");

        if (!string.IsNullOrEmpty(taxCode) && await _repo.ExistsByTaxCodeAsync(taxCode, excludeId))
            throw new ConflictException("Mã số thuế đã tồn tại");
    }

    // Tạo User role Customer gắn với hồ sơ khách hàng.
    private async Task<User> CreateCustomerUserAsync(
        string username,
        string password,
        string email,
        string fullName,
        string? phone,
        bool isActive)
    {
        if (await _userRepo.ExistsByUsernameAsync(username))
            throw new ConflictException("Username đã tồn tại");

        if (await _userRepo.ExistsByEmailAsync(email))
            throw new ConflictException("Email đã được dùng bởi tài khoản khác");

        var role = await _roleRepo.GetByNameAsync(AppRoles.Customer);
        if (role is null)
        {
            // Tự tạo role Customer nếu chưa có
            role = new Role
            {
                Name = AppRoles.Customer,
                Description = "Tài khoản khách hàng"
            };
            await _roleRepo.AddAsync(role);
            await _roleRepo.SaveChangesAsync();
        }

        return new User
        {
            Username = username,
            PasswordHash = PasswordHasher.HashPassword(password),
            FullName = fullName,
            Email = email,
            Phone = phone,
            RoleId = role.Id,
            Role = role,
            IsActive = isActive
        };
    }

    // Lấy entity Customer hoặc ném lỗi.
    private async Task<Customer> GetCustomerAsync(int id)
    {
        if (id <= 0)
            throw new ValidationException("Id không hợp lệ.");

        var customer = await _repo.GetByIdAsync(id);
        if (customer is null)
            throw new NotFoundException("Không tìm thấy khách hàng");

        return customer;
    }

    private static CustomerResponseDto Map(Customer c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        ContactName = c.ContactName,
        Phone = c.Phone,
        Email = c.Email,
        Address = c.Address,
        TaxCode = c.TaxCode,
        Note = c.Note,
        AvatarUrl = c.AvatarUrl,
        IsActive = c.IsActive,
        UserId = c.UserId,
        Username = c.User?.Username,
        RoleName = c.User?.Role?.Name,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };
}
