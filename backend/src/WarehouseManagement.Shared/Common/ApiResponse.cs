namespace WarehouseManagement.Shared;

/// <summary>
/// KHUÔN JSON thống nhất cho mọi API.
///
/// Ví dụ thành công:
/// { "success": true, "message": "Thành công", "data": { ... } }
///
/// Ví dụ lỗi:
/// { "success": false, "message": "Không tìm thấy sản phẩm", "data": null }
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    /// <summary>Tạo response thành công.</summary>
    public static ApiResponse<T> Ok(T? data, string message = "Thành công") => new()
    {
        Success = true,
        Message = message,
        Data = data
    };

    /// <summary>Tạo response thất bại (không kèm data).</summary>
    public static ApiResponse<T> Fail(string message) => new()
    {
        Success = false,
        Message = message
    };
}
