// Lớp Application — dịch vụ nghiệp vụ phiếu nhập kho (Draft → Confirm cộng tồn / Cancel đảo tồn).
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Services;

public class ImportReceiptService : IImportReceiptService
{
    private readonly IImportReceiptRepository _repo;
    private readonly IWarehouseRepository _warehouseRepo;
    private readonly ISupplierRepository _supplierRepo;
    private readonly IProductRepository _productRepo;
    private readonly IStockMovementService _movement;

    public ImportReceiptService(
        IImportReceiptRepository repo,
        IWarehouseRepository warehouseRepo,
        ISupplierRepository supplierRepo,
        IProductRepository productRepo,
        IStockMovementService movement)
    {
        _repo = repo;
        _warehouseRepo = warehouseRepo;
        _supplierRepo = supplierRepo;
        _productRepo = productRepo;
        _movement = movement;
    }

    public async Task<ImportReceiptResponseDto> GetByIdAsync(int id)
        => Map(await GetReceiptAsync(id));

    public async Task<PagedResult<ImportReceiptResponseDto>> GetAllAsync(
        PaginationQuery query,
        int? warehouseId = null,
        int? supplierId = null,
        ImportReceiptStatus? status = null)
    {
        var paged = await _repo.GetPagedAsync(
            query.Search?.Trim(),
            warehouseId,
            supplierId,
            status,
            query.PageNumber,
            query.PageSize);

        return new PagedResult<ImportReceiptResponseDto>
        {
            Items = paged.Items.Select(Map).ToList(),
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalRecords = paged.TotalRecords
        };
    }

    public async Task<ImportReceiptResponseDto> CreateAsync(ImportReceiptRequestDto dto)
    {
        await EnsureHeaderRefsAsync(dto.WarehouseId, dto.SupplierId);
        var details = await BuildDetailsAsync(dto.Details);

        var receipt = new ImportReceipt
        {
            Code = await GenerateCodeAsync(),
            ReceiptDate = dto.ReceiptDate?.ToUniversalTime() ?? DateTime.UtcNow,
            WarehouseId = dto.WarehouseId,
            SupplierId = dto.SupplierId,
            Note = dto.Note?.Trim(),
            Status = ImportReceiptStatus.Draft,
            Details = details
        };

        await _repo.AddAsync(receipt);
        await _repo.SaveChangesAsync();

        return Map(await GetReceiptAsync(receipt.Id));
    }

    public async Task<ImportReceiptResponseDto> UpdateAsync(int id, ImportReceiptRequestDto dto)
    {
        var receipt = await GetReceiptAsync(id);
        EnsureDraft(receipt);

        await EnsureHeaderRefsAsync(dto.WarehouseId, dto.SupplierId);
        var details = await BuildDetailsAsync(dto.Details);

        // Thay toàn bộ chi tiết (chỉ khi còn Draft)
        if (receipt.Details.Count > 0)
            _repo.RemoveDetails(receipt.Details.ToList());

        receipt.WarehouseId = dto.WarehouseId;
        receipt.SupplierId = dto.SupplierId;
        receipt.ReceiptDate = dto.ReceiptDate?.ToUniversalTime() ?? receipt.ReceiptDate;
        receipt.Note = dto.Note?.Trim();
        receipt.UpdatedAt = DateTime.UtcNow;
        receipt.Details = details;

        _repo.Update(receipt);
        await _repo.SaveChangesAsync();

        return Map(await GetReceiptAsync(receipt.Id));
    }

    // Xác nhận phiếu → cộng tồn kho
    public async Task<ImportReceiptResponseDto> ConfirmAsync(int id)
    {
        var receipt = await GetReceiptAsync(id);
        EnsureDraft(receipt);

        if (receipt.Details.Count == 0)
            throw new ValidationException("Phiếu nhập không có chi tiết.");

        foreach (var line in receipt.Details)
        {
            await _movement.ApplyDeltaAsync(
                receipt.WarehouseId,
                line.ProductId,
                line.Quantity,
                StockTransactionType.Import,
                StockReferenceTypes.ImportReceipt,
                receipt.Id,
                receipt.Code,
                $"Nhập kho {receipt.Code}");
        }

        receipt.Status = ImportReceiptStatus.Confirmed;
        receipt.ConfirmedAt = DateTime.UtcNow;
        receipt.UpdatedAt = DateTime.UtcNow;

        _repo.Update(receipt);
        await _repo.SaveChangesAsync();

        return Map(await GetReceiptAsync(receipt.Id));
    }

    // Hủy phiếu:
    // - Draft: chỉ đổi trạng thái
    // - Confirmed: trừ lại tồn (không cho âm)
    public async Task<ImportReceiptResponseDto> CancelAsync(int id)
    {
        var receipt = await GetReceiptAsync(id);

        if (receipt.Status == ImportReceiptStatus.Cancelled)
            throw new ConflictException("Phiếu nhập đã bị hủy");

        if (receipt.Status == ImportReceiptStatus.Confirmed)
        {
            foreach (var line in receipt.Details)
            {
                await _movement.ApplyDeltaAsync(
                    receipt.WarehouseId,
                    line.ProductId,
                    -line.Quantity,
                    StockTransactionType.ImportCancel,
                    StockReferenceTypes.ImportReceipt,
                    receipt.Id,
                    receipt.Code,
                    $"Hủy nhập kho {receipt.Code}");
            }
        }

        receipt.Status = ImportReceiptStatus.Cancelled;
        receipt.CancelledAt = DateTime.UtcNow;
        receipt.UpdatedAt = DateTime.UtcNow;

        _repo.Update(receipt);
        await _repo.SaveChangesAsync();

        return Map(await GetReceiptAsync(receipt.Id));
    }

    public async Task DeleteAsync(int id)
    {
        var receipt = await GetReceiptAsync(id);
        EnsureDraft(receipt);

        _repo.Delete(receipt);
        await _repo.SaveChangesAsync();
    }

    private async Task EnsureHeaderRefsAsync(int warehouseId, int supplierId)
    {
        var warehouse = await _warehouseRepo.GetByIdAsync(warehouseId)
            ?? throw new ValidationException("WarehouseId không tồn tại.");

        if (!warehouse.IsActive)
            throw new ValidationException("Kho hàng đang ngừng hoạt động.");

        var supplier = await _supplierRepo.GetByIdAsync(supplierId)
            ?? throw new ValidationException("SupplierId không tồn tại.");

        if (!supplier.IsActive)
            throw new ValidationException("Nhà cung cấp đang ngừng hoạt động.");
    }

    private async Task<List<ImportReceiptDetail>> BuildDetailsAsync(List<ImportReceiptDetailRequestDto> lines)
    {
        var result = new List<ImportReceiptDetail>();

        foreach (var line in lines)
        {
            var product = await _productRepo.GetByIdAsync(line.ProductId)
                ?? throw new ValidationException($"ProductId {line.ProductId} không tồn tại.");

            if (!product.IsActive)
                throw new ValidationException($"Sản phẩm {product.Sku} đang ngừng hoạt động.");

            result.Add(new ImportReceiptDetail
            {
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                LineTotal = Math.Round(line.Quantity * line.UnitPrice, 2),
                Note = line.Note?.Trim()
            });
        }

        return result;
    }

    private async Task<string> GenerateCodeAsync()
    {
        var prefix = $"PN-{DateTime.UtcNow:yyyyMMdd}-";
        var count = await _repo.CountByCodePrefixAsync(prefix);
        return $"{prefix}{(count + 1):D4}";
    }

    private static void EnsureDraft(ImportReceipt receipt)
    {
        if (receipt.Status != ImportReceiptStatus.Draft)
            throw new ConflictException("Chỉ được thao tác khi phiếu còn ở trạng thái Draft.");
    }

    private async Task<ImportReceipt> GetReceiptAsync(int id)
    {
        if (id <= 0)
            throw new ValidationException("Id không hợp lệ.");

        var receipt = await _repo.GetByIdAsync(id);
        if (receipt is null)
            throw new NotFoundException("Không tìm thấy phiếu nhập kho");

        return receipt;
    }

    private static ImportReceiptResponseDto Map(ImportReceipt r)
    {
        var details = r.Details
            .OrderBy(x => x.Id)
            .Select(d => new ImportReceiptDetailResponseDto
            {
                Id = d.Id,
                ProductId = d.ProductId,
                ProductSku = d.Product?.Sku,
                ProductName = d.Product?.Name,
                UnitSymbol = d.Product?.Unit?.Symbol,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                LineTotal = d.LineTotal,
                Note = d.Note
            })
            .ToList();

        return new ImportReceiptResponseDto
        {
            Id = r.Id,
            Code = r.Code,
            ReceiptDate = r.ReceiptDate,
            WarehouseId = r.WarehouseId,
            WarehouseCode = r.Warehouse?.Code,
            WarehouseName = r.Warehouse?.Name,
            SupplierId = r.SupplierId,
            SupplierName = r.Supplier?.Name,
            Status = r.Status.ToString(),
            Note = r.Note,
            TotalAmount = details.Sum(x => x.LineTotal),
            TotalQuantity = details.Sum(x => x.Quantity),
            ConfirmedAt = r.ConfirmedAt,
            CancelledAt = r.CancelledAt,
            Details = details,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };
    }
}
