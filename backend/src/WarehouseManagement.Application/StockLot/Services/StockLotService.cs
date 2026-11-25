using System.Text.Json;
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Services;

public class StockLotService : IStockLotService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly IStockLotRepository _repo;

    public StockLotService(IStockLotRepository repo) => _repo = repo;

    public async Task<PagedResult<StockLotResponseDto>> GetPagedAsync(
        PaginationQuery query,
        int? warehouseId = null,
        int? productId = null)
    {
        var paged = await _repo.GetPagedAsync(
            query.Search?.Trim(), warehouseId, productId, query.PageNumber, query.PageSize);
        return new PagedResult<StockLotResponseDto>
        {
            Items = paged.Items.Select(Map).ToList(),
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalRecords = paged.TotalRecords
        };
    }

    public async Task<PagedResult<StockLotResponseDto>> GetNearExpiryAsync(
        int withinDays = 90,
        int? warehouseId = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        if (withinDays < 1) withinDays = 1;
        if (withinDays > 730) withinDays = 730;

        var paged = await _repo.GetNearExpiryAsync(withinDays, warehouseId, pageNumber, pageSize);
        return new PagedResult<StockLotResponseDto>
        {
            Items = paged.Items.Select(Map).ToList(),
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalRecords = paged.TotalRecords
        };
    }

    public async Task AddFromImportAsync(
        int warehouseId,
        int productId,
        string lotNumber,
        DateTime? expiryDate,
        DateTime? manufactureDate,
        decimal quantity,
        int? importReceiptDetailId)
    {
        if (quantity <= 0) return;
        var lotKey = NormalizeLot(lotNumber);
        var existing = await _repo.GetByKeyAsync(warehouseId, productId, lotKey);
        if (existing is null)
        {
            await _repo.AddAsync(new StockLot
            {
                WarehouseId = warehouseId,
                ProductId = productId,
                LotNumber = lotKey,
                ExpiryDate = expiryDate?.Date,
                ManufactureDate = manufactureDate?.Date,
                Quantity = quantity,
                ImportReceiptDetailId = importReceiptDetailId
            });
        }
        else
        {
            existing.Quantity += quantity;
            // Cập nhật HSD nếu dòng nhập có HSD (ưu tiên hạn sớm hơn)
            if (expiryDate.HasValue)
            {
                var d = expiryDate.Value.Date;
                if (existing.ExpiryDate is null || d < existing.ExpiryDate.Value)
                    existing.ExpiryDate = d;
            }
            if (manufactureDate.HasValue && existing.ManufactureDate is null)
                existing.ManufactureDate = manufactureDate.Value.Date;
            existing.UpdatedAt = DateTime.UtcNow;
            _repo.Update(existing);
        }
    }

    public async Task DeductExactLotAsync(int warehouseId, int productId, string lotNumber, decimal quantity)
    {
        if (quantity <= 0) return;
        var lotKey = NormalizeLot(lotNumber);
        var lot = await _repo.GetByKeyAsync(warehouseId, productId, lotKey)
            ?? throw new ConflictException(
                $"Không tìm thấy lô {lotKey} (ProductId={productId}) để trừ khi hủy nhập.");

        if (lot.Quantity < quantity)
            throw new ConflictException(
                $"Lô {lotKey} không đủ để hủy nhập. Còn={lot.Quantity}, cần trừ={quantity}.");

        lot.Quantity -= quantity;
        lot.UpdatedAt = DateTime.UtcNow;
        _repo.Update(lot);
    }

    public async Task<string> DeductForExportAsync(
        int warehouseId,
        int productId,
        decimal quantity,
        string? preferLot)
    {
        if (quantity <= 0)
            return "[]";

        var lots = await _repo.GetAvailableLotsAsync(warehouseId, productId);
        // Tồn cũ (trước khi có StockLot): bỏ qua FEFO, chỉ trừ WarehouseStock.
        if (lots.Count == 0)
            return "[]";

        // Ưu tiên lô chỉ định (nếu có), sau đó FEFO: hạn gần nhất trước, null HSD xếp cuối.
        IEnumerable<StockLot> ordered = lots;
        if (!string.IsNullOrWhiteSpace(preferLot))
        {
            var key = NormalizeLot(preferLot);
            ordered = lots
                .OrderByDescending(x => string.Equals(x.LotNumber, key, StringComparison.OrdinalIgnoreCase))
                .ThenBy(x => x.ExpiryDate ?? DateTime.MaxValue)
                .ThenBy(x => x.Id);
        }
        else
        {
            ordered = lots
                .OrderBy(x => x.ExpiryDate ?? DateTime.MaxValue)
                .ThenBy(x => x.Id);
        }

        var remain = quantity;
        var allocations = new List<LotAlloc>();
        foreach (var lot in ordered)
        {
            if (remain <= 0) break;
            if (lot.Quantity <= 0) continue;

            var take = Math.Min(lot.Quantity, remain);
            lot.Quantity -= take;
            lot.UpdatedAt = DateTime.UtcNow;
            _repo.Update(lot);

            allocations.Add(new LotAlloc(lot.LotNumber, take));
            remain -= take;
        }

        if (remain > 0)
            throw new ConflictException(
                $"Không đủ tồn theo lô. ProductId={productId}, còn thiếu={remain}.");

        return JsonSerializer.Serialize(allocations, JsonOpts);
    }

    public async Task RestoreFromAllocationsAsync(
        int warehouseId,
        int productId,
        string? lotAllocationsJson)
    {
        if (string.IsNullOrWhiteSpace(lotAllocationsJson))
            return;

        List<LotAlloc>? allocations;
        try
        {
            allocations = JsonSerializer.Deserialize<List<LotAlloc>>(lotAllocationsJson, JsonOpts);
        }
        catch
        {
            return;
        }

        if (allocations is null || allocations.Count == 0)
            return;

        foreach (var a in allocations)
        {
            if (a.Qty <= 0 || string.IsNullOrWhiteSpace(a.Lot)) continue;
            await AddFromImportAsync(
                warehouseId,
                productId,
                a.Lot,
                expiryDate: null,
                manufactureDate: null,
                quantity: a.Qty,
                importReceiptDetailId: null);
        }
    }

    private static string NormalizeLot(string? lotNumber)
    {
        var s = (lotNumber ?? string.Empty).Trim().ToUpperInvariant();
        return string.IsNullOrEmpty(s) ? "KHONG-LO" : s;
    }

    private static StockLotResponseDto Map(StockLot x)
    {
        var today = DateTime.UtcNow.Date;
        int? days = x.ExpiryDate.HasValue
            ? (int)(x.ExpiryDate.Value.Date - today).TotalDays
            : null;
        var alertDays = x.Product?.ExpiryAlertDays > 0 ? x.Product.ExpiryAlertDays : 90;
        var expired = days is not null && days < 0;
        var near = days is not null && days >= 0 && days <= alertDays;

        return new StockLotResponseDto
        {
            Id = x.Id,
            WarehouseId = x.WarehouseId,
            WarehouseCode = x.Warehouse?.Code,
            WarehouseName = x.Warehouse?.Name,
            ProductId = x.ProductId,
            ProductSku = x.Product?.Sku,
            ProductName = x.Product?.Name,
            UnitSymbol = x.Product?.Unit?.Symbol,
            LotNumber = x.LotNumber,
            ExpiryDate = x.ExpiryDate,
            ManufactureDate = x.ManufactureDate,
            Quantity = x.Quantity,
            DaysToExpiry = days,
            IsExpired = expired,
            IsNearExpiry = near,
            ExpiryAlertDays = alertDays,
            IsControlledSubstance = x.Product?.IsControlledSubstance ?? false,
            RequiresColdStorage = x.Product?.RequiresColdStorage ?? false
        };
    }

    private sealed record LotAlloc(string Lot, decimal Qty);
}
