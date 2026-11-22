// Lớp Application — dịch vụ áp dụng thay đổi tồn kho và ghi sổ StockTransaction.
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Services;

public class StockMovementService : IStockMovementService
{
    private readonly IWarehouseStockRepository _warehouseStockRepository;
    private readonly IStockTransactionRepository _transactionRepository;

    public StockMovementService(
        IWarehouseStockRepository warehouseStockRepository,
        IStockTransactionRepository transactionRepository)
    {
        _warehouseStockRepository = warehouseStockRepository;
        _transactionRepository = transactionRepository;
    }

    // Cộng/trừ tồn theo delta (+ nhập / - xuất); chặn âm tồn và ghi sổ.
    public async Task ApplyDeltaAsync(
        int warehouseId,
        int productId,
        decimal quantityDelta,
        StockTransactionType type,
        string referenceType,
        int? referenceId,
        string? referenceCode,
        string? note = null)
    {
        // Delta 0 không thay đổi tồn — bỏ qua
        if (quantityDelta == 0)
            return;

        var stock = await _warehouseStockRepository.GetByWarehouseAndProductAsync(warehouseId, productId);
        var quantityBefore = stock?.Quantity ?? 0m;
        var quantityAfter = quantityBefore + quantityDelta;

        // Không cho tồn âm sau khi áp dụng delta
        if (quantityAfter < 0)
            throw new ConflictException(
                $"Không đủ tồn kho. ProductId={productId}, tồn={quantityBefore}, thay đổi={quantityDelta}.");

        await CreateOrUpdateStockAsync(stock, warehouseId, productId, quantityAfter);
        await WriteTransactionAsync(warehouseId, productId, quantityDelta, quantityBefore, quantityAfter, type, referenceType, referenceId, referenceCode, note);
    }

    // Gán tồn tuyệt đối (kiểm kê / chỉnh tay); ghi sổ theo chênh lệch tính được.
    public async Task SetQuantityAsync(
        int warehouseId,
        int productId,
        decimal newQuantity,
        StockTransactionType type,
        string referenceType,
        int? referenceId,
        string? referenceCode,
        string? note = null)
    {
        if (newQuantity < 0)
            throw new ValidationException("Số lượng tồn không được âm.");

        var stock = await _warehouseStockRepository.GetByWarehouseAndProductAsync(warehouseId, productId);
        var quantityBefore = stock?.Quantity ?? 0m;
        // Chênh lệch thực tế = tồn mới - tồn cũ
        var quantityChange = newQuantity - quantityBefore;

        // Không đổi số lượng — không ghi sổ
        if (quantityChange == 0)
            return;

        await CreateOrUpdateStockAsync(stock, warehouseId, productId, newQuantity);
        await WriteTransactionAsync(warehouseId, productId, quantityChange, quantityBefore, newQuantity, type, referenceType, referenceId, referenceCode, note);
    }

    // Tạo dòng tồn mới nếu chưa có; ngược lại cập nhật số lượng.
    private async Task CreateOrUpdateStockAsync(
        WarehouseStock? stock,
        int warehouseId,
        int productId,
        decimal quantity)
    {
        if (stock is null)
        {
            await _warehouseStockRepository.AddAsync(new WarehouseStock
            {
                WarehouseId = warehouseId,
                ProductId = productId,
                Quantity = quantity
            });
        }
        else
        {
            stock.Quantity = quantity;
            stock.UpdatedAt = DateTime.UtcNow;
            _warehouseStockRepository.Update(stock);
        }
    }

    // Ghi một dòng StockTransaction vào sổ.
    private Task WriteTransactionAsync(
        int warehouseId,
        int productId,
        decimal quantityChange,
        decimal quantityBefore,
        decimal quantityAfter,
        StockTransactionType type,
        string referenceType,
        int? referenceId,
        string? referenceCode,
        string? note)
        => _transactionRepository.AddAsync(new StockTransaction
        {
            WarehouseId = warehouseId,
            ProductId = productId,
            Type = type,
            QuantityChange = quantityChange,
            QuantityBefore = quantityBefore,
            QuantityAfter = quantityAfter,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            ReferenceCode = referenceCode,
            Note = note
        });
}
