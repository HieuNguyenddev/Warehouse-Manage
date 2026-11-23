using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Shared;

/// <summary>Tham số client gửi lên khi gọi API danh sách.</summary>
public class PaginationQuery
{
    [StringLength(100, ErrorMessage = "Từ khóa tìm kiếm tối đa 100 ký tự.")]
    public string? Search { get; set; }

    /// <summary>Mặc định 1 — bỏ trống trên Swagger vẫn chạy.</summary>
    [DefaultValue(1)]
    [Range(1, int.MaxValue, ErrorMessage = "pageNumber phải >= 1.")]
    public int PageNumber { get; set; } = 1;

    /// <summary>Mặc định 10 — bỏ trống trên Swagger vẫn chạy.</summary>
    [DefaultValue(10)]
    [Range(1, 500, ErrorMessage = "pageSize phải từ 1 đến 500.")]
    public int PageSize { get; set; } = 10;
}

/// <summary>Kết quả 1 trang dữ liệu trả về client.</summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }

    /// <summary>Tổng số trang = làm tròn lên (TotalRecords / PageSize).</summary>
    public int TotalPages => PageSize == 0
        ? 0
        : (int)Math.Ceiling((double)TotalRecords / PageSize);
}
