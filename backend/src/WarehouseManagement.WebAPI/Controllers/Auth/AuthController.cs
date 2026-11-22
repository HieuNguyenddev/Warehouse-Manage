// API xác thực: đăng nhập cổng Admin và cổng Khách hàng, trả JWT.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Shared;

namespace WarehouseManagement.WebAPI.Controllers;

// Không kế thừa AdminApiController — endpoint login phải AllowAnonymous
[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // POST /api/v1/auth/admin/login
    // Mục đích: đăng nhập cổng Admin (Admin / Manager / Staff)
    // Auth: AllowAnonymous
    // Side effect: xác thực credentials, phát hành JWT (không ghi DB ngoài audit nếu có)
    [AllowAnonymous]
    [HttpPost("admin/login")]
    public async Task<IActionResult> AdminLogin([FromBody] LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto, AppPortals.Admin);
        return Ok(ApiResponse<LoginResponseDto>.Ok(result, "Đăng nhập cổng Admin thành công"));
    }

    // POST /api/v1/auth/customer/login
    // Mục đích: đăng nhập cổng Khách hàng (chỉ role Customer)
    // Auth: AllowAnonymous
    // Side effect: xác thực credentials, phát hành JWT
    [AllowAnonymous]
    [HttpPost("customer/login")]
    public async Task<IActionResult> CustomerLogin([FromBody] LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto, AppPortals.Customer);
        return Ok(ApiResponse<LoginResponseDto>.Ok(result, "Đăng nhập cổng Khách hàng thành công"));
    }
}
