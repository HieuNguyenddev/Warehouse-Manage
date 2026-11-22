using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Infrastructure.Auth;

/// <summary>
/// Tạo JWT access token cho người dùng đăng nhập.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings;

    public JwtTokenService(IOptions<JwtSettings> jwt)
    {
        _jwtSettings = jwt.Value;
    }

    /// <summary>
    /// Sinh access token chứa HMAC-SHA256; trả về thời điểm hết hạn (UTC).
    /// </summary>
    public string GenerateAccessToken(User user, out DateTime expiresAt)
    {
        expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpireMinutes);

        // Claims cơ bản: Id, Username, Email, Role
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role?.Name ?? string.Empty)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
