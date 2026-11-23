namespace WarehouseManagement.Shared;

// Tên role cố định trong hệ thống (khớp bảng Roles / JWT claim)
public static class AppRoles
{
    public const string Admin = "Admin"; // quản trị hệ thống
    public const string Manager = "Manager"; // quản lý kho
    public const string Staff = "Staff"; // nhân viên kho
    public const string Customer = "Customer"; // khoa/phòng hoặc bệnh nhân (cổng nhà thuốc BV)

    // Chuỗi Roles cho [Authorize(Roles = ...)] cổng admin
    public const string AdminPortal = Admin + "," + Manager + "," + Staff;

    // Danh sách role được vào cổng Admin
    public static readonly string[] AdminPortalRoles = [Admin, Manager, Staff];

    // Kiểm tra role có thuộc cổng admin không
    public static bool IsAdminPortalRole(string? roleName)
        => !string.IsNullOrEmpty(roleName)
           && AdminPortalRoles.Contains(roleName, StringComparer.OrdinalIgnoreCase);

    // Kiểm tra role khách hàng
    public static bool IsCustomerPortalRole(string? roleName)
        => string.Equals(roleName, Customer, StringComparison.OrdinalIgnoreCase);
}

// Tên cổng đăng nhập (frontend lưu portal để điều hướng)
public static class AppPortals
{
    public const string Admin = "admin"; // /admin
    public const string Customer = "customer"; // /store
}
