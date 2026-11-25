// Lớp Application — dịch vụ nghiệp vụ phiếu xuất kho (Draft → Confirm trừ tồn / Cancel đảo tồn).
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Services;

public class ExportReceiptService : IExportReceiptService
{
    private readonly IExportReceiptRepository _repo;
    private readonly IWarehouseRepository _warehouseRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly IProductRepository _productRepo;
    private readonly IStockMovementService _movement;
    private readonly IStockLotService _lots;

    public ExportReceiptService(
        IExportReceiptRepository repo,
        IWarehouseRepository warehouseRepo,
        ICustomerRepository customerRepo,
        IProductRepository productRepo,
        IStockMovementService movement,
        IStockLotService lots)
    {
        _repo = repo;
        _warehouseRepo = warehouseRepo;
        _customerRepo = customerRepo;
        _productRepo = productRepo;
        _movement = movement;
        _lots = lots;
    }

    public async Task<ExportReceiptResponseDto> GetByIdAsync(int id)
        => Map(await GetReceiptAsync(id));

    public async Task<PagedResult<ExportReceiptResponseDto>> GetAllAsync(
        PaginationQuery query,
        int? warehouseId = null,
        int? customerId = null,
        ExportReceiptStatus? status = null,
        ExportReceiptSource? source = null)
    {
        var paged = await _repo.GetPagedAsync(
            query.Search?.Trim(),
            warehouseId,
            customerId,
            status,
            source,
            query.PageNumber,
            query.PageSize);

        return new PagedResult<ExportReceiptResponseDto>
        {
            Items = paged.Items.Select(Map).ToList(),
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalRecords = paged.TotalRecords
        };
    }

    public async Task<ExportReceiptResponseDto> CreateAsync(ExportReceiptRequestDto dto)
    {
        await EnsureHeaderRefsAsync(dto.WarehouseId, dto.CustomerId);
        var details = await BuildDetailsAsync(dto.Details);

        var receipt = new ExportReceipt
        {
            Code = await GenerateCodeAsync(),
            ReceiptDate = dto.ReceiptDate?.ToUniversalTime() ?? DateTime.UtcNow,
            WarehouseId = dto.WarehouseId,
            CustomerId = dto.CustomerId,
            Note = dto.Note?.Trim(),
            Status = ExportReceiptStatus.Draft,
            Source = dto.Source == ExportReceiptSource.Store
                ? ExportReceiptSource.Store
                : ExportReceiptSource.Manual,
            IssuePurpose = dto.IssuePurpose,
            Details = details
        };

        await _repo.AddAsync(receipt);
        await _repo.SaveChangesAsync();

        return Map(await GetReceiptAsync(receipt.Id));
    }

    public async Task<ExportReceiptResponseDto> UpdateAsync(int id, ExportReceiptRequestDto dto)
    {
        var receipt = await GetReceiptAsync(id);
        EnsureDraft(receipt);

        await EnsureHeaderRefsAsync(dto.WarehouseId, dto.CustomerId);
        var details = await BuildDetailsAsync(dto.Details);

        if (receipt.Details.Count > 0)
            _repo.RemoveDetails(receipt.Details.ToList());

        receipt.WarehouseId = dto.WarehouseId;
        receipt.CustomerId = dto.CustomerId;
        receipt.ReceiptDate = dto.ReceiptDate?.ToUniversalTime() ?? receipt.ReceiptDate;
        receipt.Note = dto.Note?.Trim();
        receipt.IssuePurpose = dto.IssuePurpose;
        receipt.UpdatedAt = DateTime.UtcNow;
        receipt.Details = details;

        _repo.Update(receipt);
        await _repo.SaveChangesAsync();

        return Map(await GetReceiptAsync(receipt.Id));
    }

    // Xác nhận phiếu → trừ tồn (bắt buộc đủ hàng)
    public async Task<ExportReceiptResponseDto> ConfirmAsync(int id)
    {
        var receipt = await GetReceiptAsync(id);
        EnsureDraft(receipt);

        if (receipt.Details.Count == 0)
            throw new ValidationException("Phiếu xuất không có chi tiết.");

        foreach (var line in receipt.Details)
        {
            // Trừ tồn tổng
            await _movement.ApplyDeltaAsync(
                receipt.WarehouseId,
                line.ProductId,
                -line.Quantity,
                receipt.IssuePurpose == ExportIssuePurpose.Destruction
                    ? StockTransactionType.Manual
                    : StockTransactionType.Export,
                StockReferenceTypes.ExportReceipt,
                receipt.Id,
                receipt.Code,
                $"{PurposeLabel(receipt.IssuePurpose)} {receipt.Code}");

            // Trừ tồn lô FEFO (hoặc đúng lô nếu chỉ định) + lưu phân bổ để hủy xuất
            var allocJson = await _lots.DeductForExportAsync(
                receipt.WarehouseId,
                line.ProductId,
                line.Quantity,
                line.LotNumber);
            line.LotAllocationsJson = allocJson;
            if (string.IsNullOrWhiteSpace(line.LotNumber))
                line.LotNumber = FirstLotFromAlloc(allocJson);
        }

        receipt.Status = ExportReceiptStatus.Confirmed;
        receipt.ConfirmedAt = DateTime.UtcNow;
        receipt.UpdatedAt = DateTime.UtcNow;

        _repo.Update(receipt);
        await _repo.SaveChangesAsync();

        return Map(await GetReceiptAsync(receipt.Id));
    }

    // Hủy phiếu:
    // - Draft: chỉ đổi trạng thái
    // - Confirmed: cộng lại tồn
    public async Task<ExportReceiptResponseDto> CancelAsync(int id)
    {
        var receipt = await GetReceiptAsync(id);

        if (receipt.Status == ExportReceiptStatus.Cancelled)
            throw new ConflictException("Phiếu xuất đã bị hủy");

        if (receipt.Status == ExportReceiptStatus.Confirmed)
        {
            foreach (var line in receipt.Details)
            {
                await _movement.ApplyDeltaAsync(
                    receipt.WarehouseId,
                    line.ProductId,
                    line.Quantity,
                    StockTransactionType.ExportCancel,
                    StockReferenceTypes.ExportReceipt,
                    receipt.Id,
                    receipt.Code,
                    $"Hủy xuất kho {receipt.Code}");

                await _lots.RestoreFromAllocationsAsync(
                    receipt.WarehouseId,
                    line.ProductId,
                    line.LotAllocationsJson);
            }
        }

        receipt.Status = ExportReceiptStatus.Cancelled;
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

    private async Task EnsureHeaderRefsAsync(int warehouseId, int customerId)
    {
        var warehouse = await _warehouseRepo.GetByIdAsync(warehouseId)
            ?? throw new ValidationException("WarehouseId không tồn tại.");

        if (!warehouse.IsActive)
            throw new ValidationException("Kho hàng đang ngừng hoạt động.");

        var customer = await _customerRepo.GetByIdAsync(customerId)
            ?? throw new ValidationException("CustomerId không tồn tại.");

        if (!customer.IsActive)
            throw new ValidationException("Khách hàng đang ngừng hoạt động.");
    }

    private async Task<List<ExportReceiptDetail>> BuildDetailsAsync(List<ExportReceiptDetailRequestDto> lines)
    {
        var result = new List<ExportReceiptDetail>();

        foreach (var line in lines)
        {
            var product = await _productRepo.GetByIdAsync(line.ProductId)
                ?? throw new ValidationException($"ProductId {line.ProductId} không tồn tại.");

            if (!product.IsActive)
                throw new ValidationException($"Sản phẩm {product.Sku} đang ngừng hoạt động.");

            // UnitPrice trống/0 → lấy giá bán hiện tại trên Product (sau nhập hàng)
            var unitPrice = line.UnitPrice > 0 ? line.UnitPrice : product.Price;

            result.Add(new ExportReceiptDetail
            {
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                UnitPrice = unitPrice,
                LineTotal = Math.Round(line.Quantity * unitPrice, 2),
                LotNumber = string.IsNullOrWhiteSpace(line.LotNumber)
                    ? null
                    : line.LotNumber.Trim().ToUpperInvariant(),
                Note = line.Note?.Trim()
            });
        }

        return result;
    }

    private static string PurposeLabel(ExportIssuePurpose p) => p switch
    {
        ExportIssuePurpose.PatientDispense => "Cấp phát BN",
        ExportIssuePurpose.InternalTransfer => "Điều chuyển",
        ExportIssuePurpose.Destruction => "Tiêu hủy",
        ExportIssuePurpose.ReturnToSupplier => "Trả NCC",
        _ => "Lĩnh khoa"
    };

    private static string? FirstLotFromAlloc(string json)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array
                && doc.RootElement.GetArrayLength() > 0
                && doc.RootElement[0].TryGetProperty("lot", out var lot))
                return lot.GetString();
        }
        catch { /* ignore */ }
        return null;
    }

    private async Task<string> GenerateCodeAsync()
    {
        var prefix = $"PX-{DateTime.UtcNow:yyyyMMdd}-";
        var count = await _repo.CountByCodePrefixAsync(prefix);
        return $"{prefix}{(count + 1):D4}";
    }

    private static void EnsureDraft(ExportReceipt receipt)
    {
        if (receipt.Status != ExportReceiptStatus.Draft)
            throw new ConflictException("Chỉ được thao tác khi phiếu còn ở trạng thái Draft.");
    }

    private async Task<ExportReceipt> GetReceiptAsync(int id)
    {
        if (id <= 0)
            throw new ValidationException("Id không hợp lệ.");

        var receipt = await _repo.GetByIdAsync(id);
        if (receipt is null)
            throw new NotFoundException("Không tìm thấy phiếu xuất kho");

        return receipt;
    }

    private static ExportReceiptResponseDto Map(ExportReceipt r)
    {
        var details = r.Details
            .OrderBy(x => x.Id)
            .Select(d => new ExportReceiptDetailResponseDto
            {
                Id = d.Id,
                ProductId = d.ProductId,
                ProductSku = d.Product?.Sku,
                ProductName = d.Product?.Name,
                UnitSymbol = d.Product?.Unit?.Symbol,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                LineTotal = d.LineTotal,
                LotNumber = d.LotNumber,
                LotAllocationsJson = d.LotAllocationsJson,
                Note = d.Note
            })
            .ToList();

        return new ExportReceiptResponseDto
        {
            Id = r.Id,
            Code = r.Code,
            ReceiptDate = r.ReceiptDate,
            WarehouseId = r.WarehouseId,
            WarehouseCode = r.Warehouse?.Code,
            WarehouseName = r.Warehouse?.Name,
            CustomerId = r.CustomerId,
            CustomerName = r.Customer?.Name,
            Status = r.Status.ToString(),
            Source = r.Source.ToString(),
            IssuePurpose = r.IssuePurpose.ToString(),
            InvoiceCode = r.Invoice?.Code,
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
