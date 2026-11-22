namespace WarehouseManagement.Shared;

// Cấu hình JWT đọc từ section "Jwt" trong appsettings.json
public class JwtSettings
{
    public const string SectionName = "Jwt"; // tên section config

    public string Key { get; set; } = string.Empty; // secret ký token (HMAC)
    public string Issuer { get; set; } = string.Empty; // bên phát hành
    public string Audience { get; set; } = string.Empty; // audience hợp lệ
    public int ExpireMinutes { get; set; } = 60; // thời hạn token (phút)
}
