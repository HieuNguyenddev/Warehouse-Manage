using System.Security.Cryptography;
using System.Text;

namespace WarehouseManagement.Shared;

// Hash mật khẩu SHA256 — đủ cho đồ án học tập (không dùng salt/bcrypt)
public static class PasswordHasher
{
    // Băm mật khẩu → Base64 để lưu DB
    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    // So khớp mật khẩu nhập với hash đã lưu
    public static bool VerifyPassword(string password, string passwordHash)
        => HashPassword(password) == passwordHash;
}
