namespace WarehouseManagement.Domain;

/// <summary>
/// THUỐC / VẬT TƯ Y TẾ trong kho dược bệnh viện.
/// Tồn thực tế theo kho nằm ở WarehouseStock; tồn theo lô/HSD nằm ở StockLot.
/// </summary>
public class Product : BaseEntity
{
    // --- Thông tin cơ bản ---
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int MinStock { get; set; }
    public bool IsActive { get; set; } = true;

    // --- Thuộc tính dược (kho nhà thuốc BV) ---
    /// <summary>Số đăng ký thuốc (SĐK) / mã lưu hành.</summary>
    public string? RegistrationNumber { get; set; }

    /// <summary>Hoạt chất chính (INN / tên chung quốc tế).</summary>
    public string? ActiveIngredient { get; set; }

    /// <summary>Hàm lượng / nồng độ (vd: 500mg, 5mg/ml).</summary>
    public string? Strength { get; set; }

    /// <summary>Dạng bào chế (viên nén, ống tiêm, dịch truyền…).</summary>
    public string? DosageForm { get; set; }

    /// <summary>Nhà sản xuất / công ty đăng ký.</summary>
    public string? Manufacturer { get; set; }

    /// <summary>Thuốc gây nghiện / hướng thần / kiểm soát đặc biệt.</summary>
    public bool IsControlledSubstance { get; set; }

    /// <summary>Yêu cầu bảo quản lạnh (chuỗi lạnh).</summary>
    public bool RequiresColdStorage { get; set; }

    /// <summary>Số ngày cảnh báo trước khi hết hạn (mặc định 90).</summary>
    public int ExpiryAlertDays { get; set; } = 90;

    // --- Khóa ngoại ---
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public int UnitId { get; set; }
    public Unit? Unit { get; set; }

    // --- Quan hệ 1-n ---
    public ICollection<ProductImage> Images { get; set; } = [];
    public ICollection<WarehouseStock> Stocks { get; set; } = [];
    public ICollection<StockLot> Lots { get; set; } = [];
    public ICollection<ImportReceiptDetail> ImportDetails { get; set; } = [];
    public ICollection<ExportReceiptDetail> ExportDetails { get; set; } = [];
    public ICollection<StocktakeDetail> StocktakeDetails { get; set; } = [];
    public ICollection<StockTransaction> Transactions { get; set; } = [];
}
