using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        IUserRepository userRepository,
        ICustomerRepository customerRepository,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _customerRepository = customerRepository;
        _jwtTokenService = jwtTokenService;
    }

    /// <summary>
    /// Đăng nhập theo cổng.
    /// portal = "admin"  → chỉ role Admin/Manager/Staff
    /// portal = "customer" → chỉ role Customer + phải có hồ sơ Customer
    /// </summary>
    public async Task<LoginResponseDto> LoginAsync(LoginDto dto, string portal)
    {
        // B1: tìm user theo username
        var username = dto.Username.Trim();
        var user = await _userRepository.GetByUsernameAsync(username);

        // B2: sai username hoặc sai mật khẩu → cùng 1 thông báo (tránh lộ user tồn tại)
        if (user is null || !PasswordHasher.VerifyPassword(dto.Password, user.PasswordHash))
            throw new UnauthorizedException("Username hoặc mật khẩu không đúng.");

        // B3: tài khoản bị khóa?
        if (!user.IsActive)
            throw new UnauthorizedException("Tài khoản đã bị khóa.");

        var roleName = user.Role?.Name;

        // B4: đúng cổng chưa?
        if (portal == AppPortals.Admin)
        {
            if (!AppRoles.IsAdminPortalRole(roleName))
                throw new UnauthorizedException("Tài khoản này không được đăng nhập cổng Admin.");
        }
        else if (portal == AppPortals.Customer)
        {
            if (!AppRoles.IsCustomerPortalRole(roleName))
                throw new UnauthorizedException("Tài khoản này không được đăng nhập cổng Khách hàng.");
        }
        else
        {
            throw new ValidationException("Cổng đăng nhập không hợp lệ.");
        }

        // B5: cổng Customer bắt buộc có hồ sơ khách hàng đang hoạt động
        int? customerId = null;
        if (portal == AppPortals.Customer)
        {
            var customer = await _customerRepository.GetByUserIdAsync(user.Id);
            if (customer is null || !customer.IsActive)
                throw new UnauthorizedException("Tài khoản khách hàng không hợp lệ hoặc đã bị khóa.");

            customerId = customer.Id;
        }

        // B6: tạo JWT
        var token = _jwtTokenService.GenerateAccessToken(user, out var expiresAt);

        // B7: trả về token + thông tin user cho Frontend lưu session
        return new LoginResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            Portal = portal,
            CustomerId = customerId,
            User = new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                IsActive = user.IsActive,
                RoleId = user.RoleId,
                RoleName = roleName,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            }
        };
    }
}
