// Lớp Application — dịch vụ nghiệp vụ quản lý người dùng hệ thống.
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;

    public UserService(IUserRepository userRepository, IRoleRepository roleRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
    }

    // Lấy người dùng theo Id.
    public async Task<UserResponseDto> GetByIdAsync(int id)
    {
        var user = await GetUserAsync(id);
        return ToResponseDto(user);
    }

    // Lấy danh sách người dùng phân trang.
    public async Task<PagedResult<UserResponseDto>> GetAllAsync(PaginationQuery query)
    {
        var pagedResult = await _userRepository.GetPagedAsync(query.Search?.Trim(), query.PageNumber, query.PageSize);

        return new PagedResult<UserResponseDto>
        {
            Items = pagedResult.Items.Select(ToResponseDto).ToList(),
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize,
            TotalRecords = pagedResult.TotalRecords
        };
    }

    // Tạo người dùng mới (hash mật khẩu; kiểm tra trùng username/email).
    public async Task<UserResponseDto> CreateAsync(CreateUserDto dto)
    {
        var username = dto.Username.Trim();
        var email = dto.Email.Trim();

        if (await _userRepository.ExistsByUsernameAsync(username))
            throw new ConflictException("Username đã tồn tại");

        if (await _userRepository.ExistsByEmailAsync(email))
            throw new ConflictException("Email đã tồn tại");

        await EnsureRoleExistsAsync(dto.RoleId);

        var user = new User
        {
            Username = username,
            PasswordHash = PasswordHasher.HashPassword(dto.Password),
            FullName = dto.FullName.Trim(),
            Email = email,
            Phone = dto.Phone?.Trim(),
            RoleId = dto.RoleId,
            IsActive = dto.IsActive
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        // Load lại để lấy RoleName
        user = await GetUserAsync(user.Id);
        return ToResponseDto(user);
    }

    // Cập nhật người dùng; mật khẩu chỉ đổi khi DTO có Password.
    public async Task<UserResponseDto> UpdateAsync(int id, UpdateUserDto dto)
    {
        var user = await GetUserAsync(id);
        var email = dto.Email.Trim();

        if (await _userRepository.ExistsByEmailAsync(email, id))
            throw new ConflictException("Email đã tồn tại");

        await EnsureRoleExistsAsync(dto.RoleId);

        user.FullName = dto.FullName.Trim();
        user.Email = email;
        user.Phone = dto.Phone?.Trim();
        user.RoleId = dto.RoleId;
        user.IsActive = dto.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        // Chỉ hash lại khi client gửi mật khẩu mới
        if (!string.IsNullOrEmpty(dto.Password))
            user.PasswordHash = PasswordHasher.HashPassword(dto.Password);

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync();

        user = await GetUserAsync(user.Id);
        return ToResponseDto(user);
    }

    // Xóa người dùng theo Id.
    public async Task DeleteAsync(int id)
    {
        var user = await GetUserAsync(id);
        _userRepository.Delete(user);
        await _userRepository.SaveChangesAsync();
    }

    // Đảm bảo RoleId tồn tại trước khi gán.
    private async Task EnsureRoleExistsAsync(int roleId)
    {
        var role = await _roleRepository.GetByIdAsync(roleId);
        if (role is null)
            throw new NotFoundException("Không tìm thấy vai trò");
    }

    // Lấy entity User hoặc ném lỗi.
    private async Task<User> GetUserAsync(int id)
    {
        if (id <= 0)
            throw new ValidationException("Id không hợp lệ.");

        var user = await _userRepository.GetByIdAsync(id);
        if (user is null)
            throw new NotFoundException("Không tìm thấy người dùng");

        return user;
    }

    // Map entity → DTO phản hồi.
    private static UserResponseDto ToResponseDto(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        FullName = user.FullName,
        Email = user.Email,
        Phone = user.Phone,
        IsActive = user.IsActive,
        RoleId = user.RoleId,
        RoleName = user.Role?.Name,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt
    };
}
