// Lớp Application — dịch vụ checkout cửa hàng: chọn kho đủ hàng, xuất kho, lập hóa đơn.
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Services;

public class StoreCheckoutService : IStoreCheckoutService
{
    // Miễn phí ship từ ngưỡng này trở lên
    private const decimal FreeShippingThreshold = 300_000m;
    // Phí ship mặc định khi dưới ngưỡng
    private const decimal DefaultShippingFee = 15_000m;

    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IWarehouseStockRepository _warehouseStockRepository;
    private readonly IExportReceiptService _exportReceiptService;
    private readonly IInvoiceRepository _invoiceRepository;

    public StoreCheckoutService(
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IWarehouseStockRepository warehouseStockRepository,
        IExportReceiptService exportReceiptService,
        IInvoiceRepository invoiceRepository)
    {
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _warehouseStockRepository = warehouseStockRepository;
        _exportReceiptService = exportReceiptService;
        _invoiceRepository = invoiceRepository;
    }

    // Checkout: gộp giỏ → chọn kho đủ hàng → tạo+confirm phiếu xuất → lập hóa đơn COD.
    public async Task<StoreCheckoutResponseDto> CheckoutAsync(int userId, StoreCheckoutRequestDto dto)
    {
        var customer = await _customerRepository.GetByUserIdAsync(userId)
            ?? throw new UnauthorizedException("Tài khoản khách hàng không hợp lệ.");

        if (!customer.IsActive)
            throw new ConflictException("Tài khoản khách hàng đang bị khóa.");

        // Gộp cùng ProductId thành một dòng
        var mergedItems = MergeItems(dto.Items);
        if (mergedItems.Count == 0)
            throw new ValidationException("Giỏ hàng trống.");

        var products = new Dictionary<int, Product>();
        foreach (var item in mergedItems)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId)
                ?? throw new ValidationException($"Sản phẩm #{item.ProductId} không tồn tại.");

            if (!product.IsActive)
                throw new ValidationException($"Sản phẩm {product.Sku} đang ngừng bán.");

            products[product.Id] = product;
        }

        // Chọn kho có đủ tồn cho toàn bộ đơn
        var warehouseId = await ResolveWarehouseAsync(mergedItems);

        // Mã hóa đơn trước → gắn vào phiếu xuất để admin đối chiếu trên trang Xuất kho
        var invoiceCode = await GenerateInvoiceCodeAsync();
        var note = BuildStoreExportNote(invoiceCode, dto.Note);

        var exportDto = new ExportReceiptRequestDto
        {
            WarehouseId = warehouseId,
            CustomerId = customer.Id,
            ReceiptDate = DateTime.UtcNow,
            Note = note,
            Source = ExportReceiptSource.Store,
            Details = mergedItems.Select(x => new ExportReceiptDetailRequestDto
            {
                ProductId = x.ProductId,
                Quantity = x.Quantity,
                UnitPrice = products[x.ProductId].Price
            }).ToList()
        };

        // Luôn ghi phiếu xuất kho + Confirm (trừ tồn) trước khi lập hóa đơn
        var draft = await _exportReceiptService.CreateAsync(exportDto);
        ExportReceiptResponseDto confirmed;
        try
        {
            confirmed = await _exportReceiptService.ConfirmAsync(draft.Id);
        }
        catch
        {
            // Confirm thất bại → hủy draft để không để phiếu treo
            try { await _exportReceiptService.CancelAsync(draft.Id); } catch { /* ignore */ }
            throw;
        }

        var subTotal = confirmed.Details.Sum(x => x.LineTotal);
        // Miễn ship nếu đạt ngưỡng
        var shippingFee = subTotal >= FreeShippingThreshold ? 0m : DefaultShippingFee;
        var shippingAddress = string.IsNullOrWhiteSpace(dto.ShippingAddress)
            ? customer.Address
            : dto.ShippingAddress.Trim();

        var invoice = new Invoice
        {
            Code = invoiceCode,
            InvoiceDate = DateTime.UtcNow,
            CustomerId = customer.Id,
            ExportReceiptId = confirmed.Id,
            WarehouseId = confirmed.WarehouseId,
            CustomerName = customer.Name,
            CustomerPhone = customer.Phone,
            CustomerEmail = customer.Email,
            CustomerAddress = shippingAddress,
            CustomerTaxCode = customer.TaxCode,
            SubTotal = subTotal,
            ShippingFee = shippingFee,
            TotalAmount = subTotal + shippingFee,
            TotalQuantity = confirmed.TotalQuantity,
            Status = InvoiceStatus.Issued,
            PaymentMethod = "COD",
            Note = note,
            Details = confirmed.Details.Select(d => new InvoiceDetail
            {
                ProductId = d.ProductId,
                ProductSku = d.ProductSku ?? products[d.ProductId].Sku,
                ProductName = d.ProductName ?? products[d.ProductId].Name,
                UnitSymbol = d.UnitSymbol ?? products[d.ProductId].Unit?.Symbol,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                LineTotal = d.LineTotal
            }).ToList()
        };

        await _invoiceRepository.AddAsync(invoice);
        await _invoiceRepository.SaveChangesAsync();

        var saved = await _invoiceRepository.GetByIdAsync(invoice.Id)
            ?? throw new NotFoundException("Không tạo được hóa đơn.");

        // Trả lại phiếu xuất đã Confirm (có trong danh sách Xuất kho admin)
        return new StoreCheckoutResponseDto
        {
            Invoice = ToInvoiceDto(saved),
            ExportReceipt = await _exportReceiptService.GetByIdAsync(confirmed.Id)
        };
    }

    // Ghép ghi chú phiếu xuất: mã HĐ + ghi chú khách (cắt 255).
    private static string BuildStoreExportNote(string invoiceCode, string? customerNote)
    {
        var parts = new List<string> { $"Nhà thuốc BV online · {invoiceCode}" };
        if (!string.IsNullOrWhiteSpace(customerNote))
            parts.Add(customerNote.Trim());

        var note = string.Join(" · ", parts);
        return note.Length <= 255 ? note : note[..255];
    }

    // Lấy một hóa đơn thuộc chính khách đang đăng nhập.
    public async Task<InvoiceResponseDto> GetMyInvoiceAsync(int userId, int invoiceId)
    {
        var customer = await RequireCustomerAsync(userId);
        var invoice = await _invoiceRepository.GetByIdForCustomerAsync(invoiceId, customer.Id)
            ?? throw new NotFoundException("Không tìm thấy hóa đơn.");
        return ToInvoiceDto(invoice);
    }

    // Lấy danh sách hóa đơn của khách đang đăng nhập (phân trang).
    public async Task<PagedResult<InvoiceResponseDto>> GetMyInvoicesAsync(int userId, PaginationQuery query)
    {
        var customer = await RequireCustomerAsync(userId);
        var paged = await _invoiceRepository.GetPagedAsync(
            query.Search?.Trim(),
            customer.Id,
            query.PageNumber,
            query.PageSize);

        return new PagedResult<InvoiceResponseDto>
        {
            Items = paged.Items.Select(ToInvoiceDto).ToList(),
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalRecords = paged.TotalRecords
        };
    }

    // Lấy Customer theo UserId hoặc Unauthorized.
    private async Task<Customer> RequireCustomerAsync(int userId)
    {
        var customer = await _customerRepository.GetByUserIdAsync(userId)
            ?? throw new UnauthorizedException("Tài khoản khách hàng không hợp lệ.");
        return customer;
    }

    // Gộp các dòng cùng ProductId; bỏ qty/id không hợp lệ.
    private static List<StoreCheckoutItemDto> MergeItems(List<StoreCheckoutItemDto> items)
    {
        return items
            .Where(x => x.ProductId > 0 && x.Quantity > 0)
            .GroupBy(x => x.ProductId)
            .Select(g => new StoreCheckoutItemDto
            {
                ProductId = g.Key,
                Quantity = g.Sum(x => x.Quantity)
            })
            .ToList();
    }

    // Chọn kho nhỏ nhất (theo Id) đủ tồn cho mọi dòng; không thì báo thiếu chi tiết.
    private async Task<int> ResolveWarehouseAsync(List<StoreCheckoutItemDto> items)
    {
        var productIds = items.Select(x => x.ProductId).ToList();
        var stocks = await _warehouseStockRepository.GetByProductIdsAsync(productIds);

        // Nhóm tồn theo kho, sắp xếp WarehouseId tăng dần → ưu tiên kho Id nhỏ
        var byWarehouse = stocks
            .GroupBy(x => x.WarehouseId)
            .Select(g => new
            {
                WarehouseId = g.Key,
                Warehouse = g.First().Warehouse,
                Stocks = g.ToDictionary(x => x.ProductId, x => x.Quantity)
            })
            .OrderBy(x => x.WarehouseId)
            .ToList();

        foreach (var wh in byWarehouse)
        {
            // Kho phải có đủ qty cho mọi sản phẩm trong đơn
            var ok = items.All(item =>
                wh.Stocks.TryGetValue(item.ProductId, out var qty) && qty >= item.Quantity);
            if (ok)
                return wh.WarehouseId;
        }

        // Không kho nào cover đủ → tổng hợp thiếu theo SP (cộng mọi kho)
        var shortages = new List<string>();
        foreach (var item in items)
        {
            var available = stocks
                .Where(x => x.ProductId == item.ProductId)
                .Sum(x => x.Quantity);
            if (available < item.Quantity)
            {
                var name = stocks.FirstOrDefault(x => x.ProductId == item.ProductId)?.Product?.Name
                    ?? $"#{item.ProductId}";
                shortages.Add($"{name}: cần {item.Quantity:0.##}, còn {available:0.##}");
            }
        }

        if (shortages.Count == 0)
            throw new ConflictException("Không có kho nào đủ hàng cho toàn bộ đơn. Vui lòng giảm số lượng hoặc tách đơn.");

        throw new ConflictException("Không đủ tồn kho: " + string.Join("; ", shortages));
    }

    // Sinh mã HD-yyyyMMdd-####.
    private async Task<string> GenerateInvoiceCodeAsync()
    {
        var prefix = $"HD-{DateTime.UtcNow:yyyyMMdd}-";
        var count = await _invoiceRepository.CountByCodePrefixAsync(prefix);
        return $"{prefix}{(count + 1):D4}";
    }

    // Map entity Invoice → DTO phản hồi.
    private static InvoiceResponseDto ToInvoiceDto(Invoice invoice)
    {
        var details = invoice.Details
            .OrderBy(x => x.Id)
            .Select(d => new InvoiceDetailResponseDto
            {
                Id = d.Id,
                ProductId = d.ProductId,
                ProductSku = d.ProductSku,
                ProductName = d.ProductName,
                UnitSymbol = d.UnitSymbol,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                LineTotal = d.LineTotal
            })
            .ToList();

        return new InvoiceResponseDto
        {
            Id = invoice.Id,
            Code = invoice.Code,
            InvoiceDate = invoice.InvoiceDate,
            CustomerId = invoice.CustomerId,
            CustomerName = invoice.CustomerName,
            CustomerPhone = invoice.CustomerPhone,
            CustomerEmail = invoice.CustomerEmail,
            CustomerAddress = invoice.CustomerAddress,
            CustomerTaxCode = invoice.CustomerTaxCode,
            ExportReceiptId = invoice.ExportReceiptId,
            ExportReceiptCode = invoice.ExportReceipt?.Code,
            WarehouseId = invoice.WarehouseId,
            WarehouseCode = invoice.Warehouse?.Code,
            WarehouseName = invoice.Warehouse?.Name,
            SubTotal = invoice.SubTotal,
            ShippingFee = invoice.ShippingFee,
            TotalAmount = invoice.TotalAmount,
            TotalQuantity = invoice.TotalQuantity,
            Status = invoice.Status.ToString(),
            PaymentMethod = invoice.PaymentMethod,
            Note = invoice.Note,
            Details = details,
            CreatedAt = invoice.CreatedAt
        };
    }
}
