namespace WarehouseManagement.Application.DTOs;

public class StockLotResponseDto
{
    public int Id { get; set; }
    public int WarehouseId { get; set; }
    public string? WarehouseCode { get; set; }
    public string? WarehouseName { get; set; }
    public int ProductId { get; set; }
    public string? ProductSku { get; set; }
    public string? ProductName { get; set; }
    public string? UnitSymbol { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public decimal Quantity { get; set; }
    public int? DaysToExpiry { get; set; }
    public bool IsExpired { get; set; }
    public bool IsNearExpiry { get; set; }
    public int ExpiryAlertDays { get; set; }
    public bool IsControlledSubstance { get; set; }
    public bool RequiresColdStorage { get; set; }
}
