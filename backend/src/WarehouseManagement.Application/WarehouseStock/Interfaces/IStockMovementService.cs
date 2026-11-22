// Lớp Application — hợp đồng dịch vụ áp dụng thay đổi tồn kho + ghi StockTransaction.
using WarehouseManagement.Domain;

namespace WarehouseManagement.Application.Interfaces;

public interface IStockMovementService
{
    // Cộng/trừ tồn theo delta (+ nhập / - xuất) và ghi sổ giao dịch.
    Task ApplyDeltaAsync(
        int warehouseId,
        int productId,
        decimal quantityDelta,
        StockTransactionType type,
        string referenceType,
        int? referenceId,
        string? referenceCode,
        string? note = null);

    // Gán tồn tuyệt đối (kiểm kê / chỉnh tay) và ghi sổ theo chênh lệch.
    Task SetQuantityAsync(
        int warehouseId,
        int productId,
        decimal newQuantity,
        StockTransactionType type,
        string referenceType,
        int? referenceId,
        string? referenceCode,
        string? note = null);
}
