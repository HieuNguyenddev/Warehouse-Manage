using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Infrastructure.Persistence;

/// <summary>
/// Seed role/user đăng nhập + dữ liệu demo nghiệp vụ kho nhà thuốc bệnh viện.
/// Mỗi bảng nghiệp vụ: <see cref="Target"/> bản ghi (mặc định 500) để test phân trang / filter.
/// </summary>
public static class DataSeeder
{
    private const int Target = 500; // Số bản ghi mục tiêu mỗi bảng demo
    private const string DemoPassword = "123456"; // Mật khẩu demo cho mọi user seed

    // Dữ liệu mẫu tên/địa chỉ/danh mục (quay vòng khi sinh bản ghi kho dược).
    private static readonly string[] LastNames =
    [
        "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng",
        "Bùi", "Đỗ", "Hồ", "Ngô", "Dương", "Lý", "Đinh", "Trịnh", "Mai", "Tô"
    ];

    private static readonly string[] MidNames =
        ["Văn", "Thị", "Đức", "Minh", "Hoàng", "Quốc", "Thanh", "Hữu", "Kim", "Anh"];

    private static readonly string[] FirstNames =
    [
        "An", "Bình", "Cường", "Dũng", "Em", "Phúc", "Giang", "Hà", "Hùng", "Khoa",
        "Lan", "Mai", "Nam", "Oanh", "Phong", "Quang", "Sơn", "Tâm", "Uyên", "Việt",
        "Yến", "Hương", "Tuấn", "Linh", "Hải", "Trang", "Đạt", "Nhung", "Long", "Thảo"
    ];

    private static readonly string[] Cities =
    [
        "Hà Nội", "Hồ Chí Minh", "Đà Nẵng", "Hải Phòng", "Cần Thơ", "Biên Hòa", "Nha Trang",
        "Huế", "Vũng Tàu", "Buôn Ma Thuột", "Đà Lạt", "Qui Nhơn", "Thái Nguyên", "Nam Định",
        "Vinh", "Hạ Long", "Thanh Hóa", "Pleiku", "Mỹ Tho", "Rạch Giá"
    ];

    private static readonly string[] Streets =
    [
        "Nguyễn Trãi", "Lê Lợi", "Trần Hưng Đạo", "Hai Bà Trưng", "Nguyễn Huệ", "Cách Mạng Tháng Tám",
        "Lý Thường Kiệt", "Phạm Văn Đồng", "Nguyễn Văn Cừ", "Điện Biên Phủ", "Hoàng Hoa Thám",
        "Trường Chinh", "Tô Hiến Thành", "Nguyễn Thị Minh Khai", "Võ Văn Tần"
    ];

    // Nhóm thuốc / vật tư theo nghiệp vụ nhà thuốc bệnh viện.
    private static readonly string[] CategoryNames =
    [
        "Kháng sinh", "Giảm đau hạ sốt", "Kháng viêm", "Tim mạch", "Tiêu hóa",
        "Hô hấp", "Thần kinh", "Nội tiết", "Vitamin khoáng chất", "Dịch truyền điện giải",
        "Gây mê hồi sức", "Chống dị ứng", "Da liễu", "Mắt tai mũi họng", "Sản phụ khoa",
        "Ung thư hóa chất", "Thuốc tiêm", "Thuốc uống viên", "Thuốc siro hỗn dịch", "Thuốc bôi ngoài da",
        "Vắc xin sinh phẩm", "Vật tư tiêu hao", "Dụng cụ y tế", "Hóa chất xét nghiệm", "Thuốc hướng thần",
        "Thuốc gây nghiện kiểm soát", "Thuốc cấp cứu", "Dinh dưỡng lâm sàng", "Kháng nấm kháng virus", "Kháng đông cầm máu"
    ];

    // Khoa/phòng nhận thuốc (đóng vai "khách hàng" nội bộ bệnh viện).
    private static readonly string[] HospitalDepartments =
    [
        "Khoa Nội tổng hợp", "Khoa Ngoại tổng hợp", "Khoa Nhi", "Khoa Sản", "Khoa Cấp cứu",
        "Khoa Hồi sức ICU", "Khoa Tim mạch", "Khoa Hô hấp", "Khoa Thần kinh", "Khoa Ung bướu",
        "Khoa Truyền nhiễm", "Khoa Mắt", "Khoa Tai mũi họng", "Khoa Da liễu", "Khoa Thận lọc máu",
        "Phòng mổ trung tâm", "Nhà thuốc bệnh nhân ngoại trú", "Khoa Xét nghiệm", "Khoa Chẩn đoán hình ảnh",
        "Khoa Phục hồi chức năng"
    ];

    // Nhà cung cấp dược / vật tư y tế.
    private static readonly string[] PharmaSupplierBases =
    [
        "Công ty Dược phẩm Trung ương", "Dược Hậu Giang", "Pymepharco", "Imexpharm",
        "Stada Việt Nam", "Sanofi Việt Nam", "GSK Pharma", "AstraZeneca VN",
        "Roche Diagnostics", "Baxter Healthcare", "B.Braun Medical", "Nipro Medical",
        "Medtronic Việt Nam", "Công ty Vật tư y tế Việt Đức", "Pharmacity Distribution",
        "Diethelm Healthcare", "Zuellig Pharma", "DKSH Healthcare", "Mega Lifesciences",
        "Domesco"
    ];

    // Đơn vị đóng gói phổ biến trong kho dược / vật tư y tế.
    private static readonly (string Name, string Symbol)[] PharmacyUnitDefs =
    [
        ("Viên", "vien"), ("Vỉ", "vi"), ("Hộp", "hop"), ("Chai", "chai"),
        ("Lọ", "lo"), ("Ống", "ong"), ("Tuýp", "tuyp"), ("Gói", "goi"),
        ("Túi", "tui"), ("Lọ xịt", "xit"), ("Bình dịch truyền", "binh"),
        ("Mililít", "ml"), ("Lít", "l"), ("Gram", "g"), ("Kilogram", "kg"),
        ("Đơn vị quốc tế", "iu"), ("Ống tiêm", "syr"), ("Kim tiêm", "kim"),
        ("Cuộn", "cuon"), ("Tờ", "to"), ("Bộ", "bo"), ("Cái", "cai"),
        ("Thùng", "thung"), ("Thùng 12 hộp", "th12"), ("Thùng 24 hộp", "th24"),
        ("Lốc 6", "loc6"), ("Lốc 10", "loc10")
    ];

    /// <summary>
    /// Điểm vào seed: migrate DB → role/user → dữ liệu demo → đảm bảo file ảnh tồn tại.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

        await db.Database.MigrateAsync(); // Áp dụng migration chưa chạy
        await SeedRolesAndLoginUsersAsync(db);
        await SeedDemoDataAsync(db);
        // Top-up Products tới Target (idempotent) + ảnh hiển thị được trên UI
        await EnsureDemoProductsAsync(db);
        // Đổi nhãn / tên toàn bộ bản ghi demo sang ngữ cảnh kho nhà thuốc bệnh viện
        // (kể cả DB đã seed trước đó với dữ liệu siêu thị / thực phẩm).
        await RefreshPharmacyDomainLabelsAsync(db);
        await EnsureImportPurchaseCostsAndInvoicesAsync(db);
        await EnsureImportSalePricesSyncedAsync(db);
        await EnsureStorefrontUniqueProductsAsync(db);
        await EnsureProductImagesReadyAsync(db, env.ContentRootPath);
    }

    /// <summary>
    /// Đồng bộ giá bán:
    /// 1) Backfill ImportReceiptDetails.SalePrice (= Product.Price) nếu đang = 0
    /// 2) Cập nhật Product.Price theo giá bán mới nhất trên phiếu nhập đã Confirm
    ///    → trang người dùng luôn thấy giá bán sau nhập hàng.
    /// </summary>
    private static async Task EnsureImportSalePricesSyncedAsync(ApplicationDbContext db)
    {
        // (1) Backfill SalePrice trên dòng nhập cũ
        var orphanLines = await db.ImportReceiptDetails
            .Include(x => x.Product)
            .Where(x => x.SalePrice <= 0 && x.Product != null)
            .ToListAsync();
        foreach (var line in orphanLines)
        {
            // Giá bán mặc định = giá danh mục hiện tại (Product.Price)
            line.SalePrice = Math.Round(line.Product!.Price, 2);
            if (line.SalePrice <= 0 && line.UnitPrice > 0)
                line.SalePrice = Math.Round(line.UnitPrice * 1.3m, 0); // fallback markup
        }

        if (orphanLines.Count > 0)
            await db.SaveChangesAsync();

        // Seed mới: luôn gắn SalePrice khi tạo dòng (xem vòng tạo PN-SEED bên dưới).
        // (2) Product.Price ← SalePrice mới nhất từ phiếu nhập Confirmed
        var latestSales = await db.ImportReceiptDetails
            .AsNoTracking()
            .Where(x => x.ImportReceipt != null
                        && x.ImportReceipt.Status == ImportReceiptStatus.Confirmed
                        && x.SalePrice > 0)
            .Select(x => new
            {
                x.ProductId,
                x.SalePrice,
                ConfirmedAt = x.ImportReceipt!.ConfirmedAt ?? x.ImportReceipt.CreatedAt,
                x.Id
            })
            .ToListAsync();

        if (latestSales.Count == 0)
            return;

        var byProduct = latestSales
            .GroupBy(x => x.ProductId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.ConfirmedAt).ThenByDescending(x => x.Id).First().SalePrice);

        var productIds = byProduct.Keys.ToList();
        var products = await db.Products.Where(x => productIds.Contains(x.Id)).ToListAsync();
        var changed = false;
        foreach (var p in products)
        {
            if (!byProduct.TryGetValue(p.Id, out var sale) || sale <= 0)
                continue;
            var rounded = Math.Round(sale, 2);
            if (p.Price == rounded)
                continue;
            p.Price = rounded;
            p.UpdatedAt = DateTime.UtcNow;
            changed = true;
        }

        if (changed)
            await db.SaveChangesAsync();
    }

    /// <summary>
    /// Ẩn SP mã lệch (SP-001…) khỏi cổng người dùng; chỉ giữ SKU SP#### active.
    /// </summary>
    private static async Task EnsureStorefrontUniqueProductsAsync(ApplicationDbContext db)
    {
        var odd = await db.Products
            .Where(x => x.IsActive && !EF.Functions.Like(x.Sku, "SP[0-9][0-9][0-9][0-9]"))
            .ToListAsync();
        foreach (var p in odd)
            p.IsActive = false;

        // Nếu trùng tên (không phân biệt hoa thường) → giữ Id nhỏ nhất, tắt các bản còn lại
        var dups = await db.Products
            .Where(x => x.IsActive)
            .GroupBy(x => x.Name)
            .Where(g => g.Count() > 1)
            .Select(g => new { KeepId = g.Min(x => x.Id), Ids = g.Select(x => x.Id).ToList() })
            .ToListAsync();

        foreach (var g in dups)
        {
            foreach (var id in g.Ids.Where(id => id != g.KeepId))
            {
                var p = await db.Products.FindAsync(id);
                if (p is not null) p.IsActive = false;
            }
        }

        if (odd.Count > 0 || dups.Count > 0)
            await db.SaveChangesAsync();
    }

    /// <summary>
    /// Backfill chi phí nhập + mã hóa đơn mua hàng cho phiếu Confirmed cũ (idempotent).
    /// </summary>
    private static async Task EnsureImportPurchaseCostsAndInvoicesAsync(ApplicationDbContext db)
    {
        var receipts = await db.ImportReceipts
            .Include(x => x.Details)
            .Where(x => x.Status == ImportReceiptStatus.Confirmed &&
                        (x.PurchaseInvoiceCode == null || x.PurchaseInvoiceCode == string.Empty))
            .OrderBy(x => x.Id)
            .ToListAsync();

        if (receipts.Count == 0)
            return;

        var seq = 1;
        foreach (var r in receipts)
        {
            var goods = r.Details.Sum(d => d.LineTotal);
            if (r.ShippingFee <= 0)
                r.ShippingFee = 50_000m + (r.Id % 7) * 10_000m;
            if (r.TradeDiscount <= 0 && goods > 0)
                r.TradeDiscount = Math.Round(goods * 0.02m, 0);
            if (r.NonRefundableTax <= 0 && goods > 0)
                r.NonRefundableTax = Math.Round(goods * 0.05m, 0);

            r.PurchaseInvoiceCode = $"HDN-SEED-{seq:D4}";
            seq++;
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Cập nhật tên/mô tả các bảng demo sang kho dược bệnh viện (idempotent, chạy mỗi lần start).
    /// Không xóa FK / phiếu — chỉ đổi nhãn hiển thị và một số note.
    /// Dùng 2 pha tạm (tmp-*) rồi tên cuối để tránh lỗi unique index khi đổi tên hàng loạt.
    /// </summary>
    private static async Task RefreshPharmacyDomainLabelsAsync(ApplicationDbContext db)
    {
        // Roles (chỉ Description)
        var roleUpdates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [AppRoles.Admin] = "Quản trị hệ thống kho dược bệnh viện",
            [AppRoles.Manager] = "Trưởng khoa / quản lý kho dược",
            [AppRoles.Staff] = "Dược sĩ / nhân viên kho thuốc",
            [AppRoles.Customer] = "Khoa/phòng nhận thuốc hoặc cổng nhà thuốc BV"
        };
        foreach (var role in await db.Roles.ToListAsync())
        {
            if (roleUpdates.TryGetValue(role.Name, out var desc))
                role.Description = desc;
        }

        await db.SaveChangesAsync();

        // --- Pha 1: tên tạm duy nhất ---
        var categories = await db.Categories.OrderBy(x => x.Id).ToListAsync();
        for (var i = 0; i < categories.Count; i++)
            categories[i].Name = $"tmp-cat-{categories[i].Id}";

        var units = await db.Units.OrderBy(x => x.Id).ToListAsync();
        for (var i = 0; i < units.Count; i++)
        {
            units[i].Name = $"tmp-unit-{units[i].Id}";
            units[i].Symbol = $"t{units[i].Id}"; // Symbol cũng unique → đổi tạm trước
        }

        var warehouses = await db.Warehouses.OrderBy(x => x.Id).ToListAsync();
        for (var i = 0; i < warehouses.Count; i++)
            warehouses[i].Name = $"tmp-wh-{warehouses[i].Id}";

        var suppliers = await db.Suppliers.OrderBy(x => x.Id).ToListAsync();
        for (var i = 0; i < suppliers.Count; i++)
        {
            suppliers[i].Name = $"tmp-sup-{suppliers[i].Id}";
            suppliers[i].Email = $"tmp.sup.{suppliers[i].Id}@pharma.vn";
        }

        var customers = await db.Customers.OrderBy(x => x.Id).ToListAsync();
        for (var i = 0; i < customers.Count; i++)
        {
            customers[i].Name = $"tmp-cus-{customers[i].Id}";
            customers[i].Email = $"tmp.cus.{customers[i].Id}@bv-demo.vn";
        }

        // Products: tên tạm để tránh đụng unique (nếu sau này thêm) + chuẩn bị remap
        var products = await db.Products.OrderBy(x => x.Id).ToListAsync();
        for (var i = 0; i < products.Count; i++)
            products[i].Name = $"tmp-sp-{products[i].Id}";

        var staffLike = await db.Users
            .Where(x => x.Username.StartsWith("nv") || x.Username.StartsWith("kh"))
            .ToListAsync();
        foreach (var u in staffLike)
            u.Email = $"tmp.{u.Username}@bv-demo.local";

        await db.SaveChangesAsync();

        // --- Pha 2: tên kho dược cuối cùng ---
        for (var i = 0; i < categories.Count; i++)
        {
            var n = i + 1;
            var baseName = CategoryNames[i % CategoryNames.Length];
            categories[i].Name = Clip($"DM {baseName} {n:D4}", 100);
            categories[i].Description = Clip($"Nhóm {baseName} — phân loại thuốc/vật tư kho nhà thuốc bệnh viện", 255);
            categories[i].IsActive = true;
        }

        var unitDefs = PharmacyUnitDefs;
        var usedSymbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < units.Count; i++)
        {
            var n = i + 1;
            var def = unitDefs[i % unitDefs.Length];
            var symbol = n <= unitDefs.Length ? def.Symbol : $"u{n:D4}";
            if (!usedSymbols.Add(symbol))
                symbol = $"u{n:D4}";
            units[i].Name = Clip(n <= unitDefs.Length ? def.Name : $"{def.Name} #{n:D4}", 50);
            units[i].Symbol = symbol;
            units[i].Description = Clip($"Đơn vị đóng gói kho dược: {units[i].Name}", 255);
            units[i].IsActive = true;
        }

        for (var i = 0; i < warehouses.Count; i++)
        {
            var n = i + 1;
            var city = Cities[i % Cities.Length];
            var code = string.IsNullOrWhiteSpace(warehouses[i].Code) ? $"WH{n:D4}" : warehouses[i].Code;
            warehouses[i].Name = Clip(n switch
            {
                1 => $"Kho thuốc trung tâm BV {city}",
                2 => $"Kho lạnh vắc xin / sinh phẩm {city}",
                3 => $"Kho gây nghiện hướng thần {city}",
                4 => $"Kho vật tư y tế tiêu hao {city}",
                5 => $"Kho khoa dược lâm sàng {city}",
                _ => $"Kho dược {city} {code}"
            }, 150);
            warehouses[i].WarehouseType = (n % 5) switch
            {
                2 => WarehouseType.ColdChain,
                3 => WarehouseType.Controlled,
                4 => WarehouseType.MedicalSupply,
                0 => WarehouseType.Emergency,
                _ => WarehouseType.General
            };
            warehouses[i].Note = warehouses[i].WarehouseType switch
            {
                WarehouseType.ColdChain => "Kho lạnh 2-8°C (vắc xin, insulin, sinh phẩm)",
                WarehouseType.Controlled => "Kho thuốc gây nghiện / hướng thần — kiểm soát đặc biệt",
                WarehouseType.MedicalSupply => "Kho vật tư vô trùng / tiêu hao",
                WarehouseType.Emergency => "Kho cấp cứu 24/7",
                _ => "Kho thuốc thường — nhiệt độ phòng"
            };
        }

        for (var i = 0; i < suppliers.Count; i++)
        {
            var n = i + 1;
            var city = Cities[i % Cities.Length];
            var baseSup = PharmaSupplierBases[i % PharmaSupplierBases.Length];
            suppliers[i].Name = Clip($"NCC {baseSup} · {city} #{n:D4}", 150);
            suppliers[i].Email = $"ncc.duoc{n:D4}@pharma.vn";
            suppliers[i].Note = "Nhà cung cấp thuốc / vật tư y tế cho bệnh viện";
        }

        // Chỉ 1 Customer gắn user "customer1" được giữ email/tên demo cố định.
        var portalCustomerUserId = await db.Users
            .Where(x => x.Username == "customer1")
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync();

        for (var i = 0; i < customers.Count; i++)
        {
            var n = i + 1;
            var city = Cities[i % Cities.Length];
            var dept = HospitalDepartments[i % HospitalDepartments.Length];
            if (portalCustomerUserId is not null && customers[i].UserId == portalCustomerUserId)
            {
                customers[i].Name = "Khoa Nội tổng hợp — Bệnh viện Đa khoa Demo";
                customers[i].ContactName = "Phạm Điều Dưỡng trưởng";
                customers[i].Email = "khoa.noi@bv-demo.vn";
            }
            else
            {
                customers[i].Name = Clip($"{dept} · BV {city} #{n:D4}", 150);
                customers[i].Email = $"khoa{n:D4}@bv-demo.vn";
            }

            customers[i].Note = "Khoa/phòng nhận thuốc hoặc nhà thuốc vệ tinh trong bệnh viện";
        }

        // Products: gắn catalog thuốc/VT y tế.
        // Pass 1: SKU chuẩn SP0001…SP0500. Pass 2: SKU lệch (SP-001, …) lấy slot catalog còn trống.
        var catalog = PharmacyProductCatalog.All;
        var usedCatalogIdx = new HashSet<int>();
        var usedProductNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        static bool TryCanonicalSkuIndex(string? sku, int catalogCount, out int idx)
        {
            idx = -1;
            if (string.IsNullOrEmpty(sku) || sku.Length != 6 ||
                !sku.StartsWith("SP", StringComparison.OrdinalIgnoreCase))
                return false;
            if (!char.IsDigit(sku[2]) || !char.IsDigit(sku[3]) || !char.IsDigit(sku[4]) || !char.IsDigit(sku[5]))
                return false;
            if (!int.TryParse(sku.AsSpan(2), out var skuNum) || skuNum < 1 || skuNum > catalogCount)
                return false;
            idx = skuNum - 1;
            return true;
        }

        void ApplyCatalog(Product product, int idx)
        {
            var item = catalog[idx % catalog.Count];
            var name = item.Name;
            if (!usedProductNames.Add(name))
            {
                name = Clip($"{item.Name} [{product.Sku}]", 150);
                usedProductNames.Add(name);
            }

            product.Name = Clip(name, 150);
            product.Description = Clip(
                $"{item.Name} — thuốc/vật tư kho dược bệnh viện (SKU {product.Sku})", 1000);
            product.Price = item.Price;
            product.IsActive = true;
            ApplyPharmacyAttrs(product, item.Name, idx);

            if (categories.Count > 0)
                product.CategoryId = categories[item.CategoryIndex % categories.Count].Id;
            if (units.Count > 0)
                product.UnitId = units[idx % units.Count].Id;
        }

        var orphans = new List<(Product Product, int Ordinal)>();
        for (var i = 0; i < products.Count; i++)
        {
            if (TryCanonicalSkuIndex(products[i].Sku, catalog.Count, out var idx))
            {
                usedCatalogIdx.Add(idx);
                ApplyCatalog(products[i], idx);
            }
            else
            {
                orphans.Add((products[i], i));
            }
        }

        foreach (var (product, ordinal) in orphans)
        {
            var idx = -1;
            for (var k = 0; k < catalog.Count; k++)
            {
                var cand = (ordinal + k) % catalog.Count;
                if (usedCatalogIdx.Add(cand))
                {
                    idx = cand;
                    break;
                }
            }

            if (idx < 0)
                idx = ordinal % catalog.Count;
            ApplyCatalog(product, idx);
        }

        foreach (var u in staffLike)
        {
            if (u.Username.StartsWith("nv", StringComparison.OrdinalIgnoreCase))
                u.Email = $"{u.Username}@bv-duoc.local";
            else if (u.Username.StartsWith("kh", StringComparison.OrdinalIgnoreCase))
                u.Email = $"{u.Username}@bv-demo.vn";
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Đảm bảo có đủ <see cref="Target"/> sản phẩm demo (idempotent theo SKU SP0001…SP0500).
    /// </summary>
    private static async Task EnsureDemoProductsAsync(ApplicationDbContext db)
    {
        var categories = await db.Categories.OrderBy(x => x.Id).ToListAsync();
        var units = await db.Units.OrderBy(x => x.Id).ToListAsync();
        if (categories.Count == 0 || units.Count == 0)
            return; // Chưa có danh mục/đơn vị — chờ SeedDemoDataAsync tạo trước

        if (await db.Products.CountAsync() >= Target)
            return;

        var existingSkus = (await db.Products.AsNoTracking().Select(x => x.Sku).ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var catalog = PharmacyProductCatalog.All;
        var products = new List<Product>();
        for (var i = 1; i <= Target; i++)
        {
            var sku = $"SP{i:D4}";
            if (existingSkus.Contains(sku))
                continue;

            var item = catalog[(i - 1) % catalog.Count];
            // Ưu tiên category theo nhóm thuốc trong catalog; fallback quay vòng nếu thiếu.
            var catIdx = item.CategoryIndex % categories.Count;
            var unitIdx = (i - 1) % units.Count;

            var p = new Product
            {
                Sku = sku,
                Name = Clip(item.Name, 150),
                Description = Clip($"{item.Name} — thuốc/vật tư kho dược bệnh viện (SKU {sku})", 1000),
                Price = item.Price,
                MinStock = 5 + (i % 20),
                CategoryId = categories[catIdx].Id,
                UnitId = units[unitIdx].Id,
                IsActive = true
            };
            ApplyPharmacyAttrs(p, item.Name, i - 1);
            products.Add(p);
        }

        if (products.Count == 0)
            return;

        db.Products.AddRange(products);
        await db.SaveChangesAsync();

        var newSkus = products.Select(x => x.Sku).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var savedNew = await db.Products
            .Where(x => newSkus.Contains(x.Sku))
            .OrderBy(x => x.Id)
            .ToListAsync();

        var images = new List<ProductImage>();
        foreach (var p in savedNew)
        {
            var n = int.TryParse(p.Sku.AsSpan(2), out var num) ? num : 9999;
            images.Add(new ProductImage
            {
                ProductId = p.Id,
                ImageUrl = $"/uploads/products/pharma-{p.Sku.ToLowerInvariant()}-1.jpg",
                SortOrder = 0,
                IsPrimary = true
            });

            if (n <= 50)
            {
                images.Add(new ProductImage
                {
                    ProductId = p.Id,
                    ImageUrl = $"/uploads/products/pharma-{p.Sku.ToLowerInvariant()}-2.jpg",
                    SortOrder = 1,
                    IsPrimary = false
                });
            }
        }

        db.ProductImages.AddRange(images);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Đảm bảo mọi Product có ≥1 ảnh primary và file trên đĩa khớp nhóm thuốc
    /// (ảnh Wikimedia Commons / stock theo CategoryIndex — không scrape Google).
    /// </summary>
    private static async Task EnsureProductImagesReadyAsync(ApplicationDbContext db, string contentRoot)
    {
        var webRoot = Path.Combine(contentRoot, "wwwroot");
        var productDir = Path.Combine(webRoot, "uploads", "products");
        var templateDir = Path.Combine(productDir, "templates");
        Directory.CreateDirectory(productDir);
        Directory.CreateDirectory(templateDir);

        // Tải / cache ảnh theo 30 nhóm thuốc
        await PharmacyImageSources.EnsureCategoryTemplatesAsync(templateDir);

        var packFallback = Directory.Exists(templateDir)
            ? Directory.GetFiles(templateDir, "pack-*.png").OrderBy(x => x, StringComparer.Ordinal).ToArray()
            : [];

        // Map SKU / tên → CategoryIndex trong catalog thuốc
        var catalog = PharmacyProductCatalog.All;
        int CategoryOf(Product p)
        {
            var sku = p.Sku ?? string.Empty;
            if (sku.Length == 6 && sku.StartsWith("SP", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(sku.AsSpan(2), out var n) && n >= 1 && n <= catalog.Count)
                return catalog[n - 1].CategoryIndex;

            var byName = catalog.FirstOrDefault(x =>
                p.Name.StartsWith(x.Name, StringComparison.OrdinalIgnoreCase) ||
                x.Name.StartsWith(p.Name.Split(' ')[0], StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(byName.Name))
                return byName.CategoryIndex;

            return Math.Abs(HashCode.Combine(p.Sku?.ToLowerInvariant())) % PharmacyImageSources.CategoryQueries.Length;
        }

        // ----- 1) DB: mọi SP có ảnh primary; chuẩn hoá URL pharma-*.jpg (bỏ demo-* cũ) -----
        var products = await db.Products
            .Include(x => x.Images)
            .OrderBy(x => x.Id)
            .ToListAsync();

        var dbChanged = false;
        foreach (var p in products)
        {
            var primaryUrl = $"/uploads/products/pharma-{p.Sku.ToLowerInvariant()}-1.jpg";
            if (p.Images.Count == 0)
            {
                p.Images.Add(new ProductImage
                {
                    ImageUrl = primaryUrl,
                    SortOrder = 0,
                    IsPrimary = true
                });
                dbChanged = true;
                continue;
            }

            // Đổi demo-* / .png cũ → pharma-*.jpg (URL mới để tránh cache ảnh người/stock)
            foreach (var img in p.Images)
            {
                var url = img.ImageUrl ?? string.Empty;
                if (!url.Contains("/uploads/products/", StringComparison.OrdinalIgnoreCase))
                    continue;

                var updated = url
                    .Replace("/uploads/products/demo-", "/uploads/products/pharma-", StringComparison.OrdinalIgnoreCase);
                if (updated.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    updated = Path.ChangeExtension(updated, ".jpg")!.Replace('\\', '/');

                if (!string.Equals(updated, url, StringComparison.Ordinal))
                {
                    img.ImageUrl = updated;
                    dbChanged = true;
                }
            }

            if (!p.Images.Any(x => x.IsPrimary))
            {
                p.Images.OrderBy(x => x.SortOrder).ThenBy(x => x.Id).First().IsPrimary = true;
                dbChanged = true;
            }
        }

        if (dbChanged)
            await db.SaveChangesAsync();

        // ----- 2) Đĩa: ghi đè file demo theo nhóm thuốc (luôn refresh lần seed) -----
        var tinyFallback = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

        foreach (var p in products)
        {
            var cat = CategoryOf(p);
            // Ảnh đúng nhóm thuốc/vật tư (không trộn ảnh không liên quan)
            var template = PharmacyImageSources.TemplatePathForCategory(templateDir, cat);
            if (!File.Exists(template) || new FileInfo(template).Length < 3000)
            {
                if (packFallback.Length > 0)
                    template = packFallback[cat % packFallback.Length];
                else
                    template = string.Empty;
            }

            foreach (var img in p.Images)
            {
                var url = img.ImageUrl;
                if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (url.Contains("/templates/", StringComparison.OrdinalIgnoreCase))
                    continue;

                var fullPath = Path.Combine(
                    webRoot,
                    url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                var dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                // Ảnh phụ: nhóm thuốc liền kề (vẫn trong catalog dược), không lấy ảnh bẩn
                var src = template;
                if (!img.IsPrimary)
                {
                    var alt = PharmacyImageSources.TemplatePathForCategory(templateDir, cat + 1);
                    if (File.Exists(alt) && new FileInfo(alt).Length >= 3000)
                        src = alt;
                }

                if (!string.IsNullOrEmpty(src) && File.Exists(src))
                    File.Copy(src, fullPath, overwrite: true);
                else if (!File.Exists(fullPath) || new FileInfo(fullPath).Length < 500)
                    await File.WriteAllBytesAsync(fullPath, tinyFallback);

                // Xoá file .png demo cũ nếu còn
                if (fullPath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                {
                    var legacyPng = Path.ChangeExtension(fullPath, ".png");
                    if (legacyPng is not null && File.Exists(legacyPng))
                    {
                        try { File.Delete(legacyPng); } catch { /* ignore */ }
                    }
                }
            }
        }

        // Xoá hẳn file demo-* cũ (ảnh stock người từng bị cache trên URL này)
        foreach (var legacy in Directory.EnumerateFiles(productDir, "demo-*.jpg"))
        {
            try { File.Delete(legacy); } catch { /* ignore */ }
        }

        foreach (var legacy in Directory.EnumerateFiles(productDir, "demo-*.png"))
        {
            try { File.Delete(legacy); } catch { /* ignore */ }
        }
    }

    // Roles + tài khoản đăng nhập cố định (admin / manager / staff / customer1)
    /// <summary>
    /// Tạo 4 role hệ thống và các user đăng nhập demo (kèm 1 Customer gắn customer1).
    /// </summary>
    private static async Task SeedRolesAndLoginUsersAsync(ApplicationDbContext db)
    {
        var roles = new (string Name, string Description)[]
        {
            (AppRoles.Admin, "Quản trị hệ thống kho dược bệnh viện"),
            (AppRoles.Manager, "Trưởng khoa / quản lý kho dược"),
            (AppRoles.Staff, "Dược sĩ / nhân viên kho thuốc"),
            (AppRoles.Customer, "Khoa/phòng nhận thuốc hoặc cổng nhà thuốc BV")
        };

        foreach (var (name, description) in roles)
        {
            if (!await db.Roles.AnyAsync(x => x.Name == name))
                db.Roles.Add(new Role { Name = name, Description = description });
        }

        await db.SaveChangesAsync();

        var adminRole = await db.Roles.FirstAsync(x => x.Name == AppRoles.Admin);
        var managerRole = await db.Roles.FirstAsync(x => x.Name == AppRoles.Manager);
        var staffRole = await db.Roles.FirstAsync(x => x.Name == AppRoles.Staff);
        var customerRole = await db.Roles.FirstAsync(x => x.Name == AppRoles.Customer);

        await EnsureUserAsync(db, "admin", DemoPassword, "Nguyễn Quản Trị Dược", "admin@bv-duoc.local", adminRole.Id, "0901000001");
        await EnsureUserAsync(db, "manager", DemoPassword, "Trần Trưởng Khoa Dược", "manager@bv-duoc.local", managerRole.Id, "0901000002");
        await EnsureUserAsync(db, "staff", DemoPassword, "Lê Dược sĩ Kho", "staff@bv-duoc.local", staffRole.Id, "0901000003");

        var customerUser = await EnsureUserAsync(
            db, "customer1", DemoPassword, "Phạm Điều Dưỡng trưởng", "customer1@bv-demo.vn", customerRole.Id, "0909000111");

        const string demoDeptEmail = "khoa.noi@bv-demo.vn";
        const string demoDeptName = "Khoa Nội tổng hợp — Bệnh viện Đa khoa Demo";

        var linkedCustomer = await db.Customers.FirstOrDefaultAsync(x => x.UserId == customerUser.Id);

        // Name/Email unique: nhường slot cho Customer gắn portal customer1.
        // Lưu ý: UserId NULL không khớp `!= id` trong SQL → phải OR HasValue==false.
        foreach (var other in await db.Customers
                     .Where(x => (x.UserId == null || x.UserId != customerUser.Id) &&
                                 (x.Email == demoDeptEmail || x.Name == demoDeptName))
                     .ToListAsync())
        {
            other.Name = Clip($"Khoa demo (legacy #{other.Id})", 150);
            other.Email = $"khoa.legacy.{other.Id}@bv-demo.vn";
        }

        if (linkedCustomer is null)
        {
            db.Customers.Add(new Customer
            {
                Name = demoDeptName,
                ContactName = "Phạm Điều Dưỡng trưởng",
                Email = demoDeptEmail,
                Phone = "0909000111",
                Address = "123 Nguyễn Trãi, Thanh Xuân, Hà Nội",
                TaxCode = "0101234567",
                Note = "Khoa/phòng nhận thuốc trong bệnh viện",
                IsActive = true,
                UserId = customerUser.Id
            });
        }
        else
        {
            linkedCustomer.Name = demoDeptName;
            linkedCustomer.ContactName = "Phạm Điều Dưỡng trưởng";
            linkedCustomer.Email = demoDeptEmail;
            linkedCustomer.Note = "Khoa/phòng nhận thuốc trong bệnh viện";
        }

        await db.SaveChangesAsync();
    }

    // Target bản ghi / bảng nghiệp vụ — idempotent top-up (có thể chạy lại)
    /// <summary>
    /// dữ liệu demo Categories → Units → Warehouses → Suppliers → Users →
    /// Customers → Products → Stocks → Import/Export/Stocktake + StockTransactions.
    /// </summary>
    private static async Task SeedDemoDataAsync(ApplicationDbContext db)
    {
        var adminRole = await db.Roles.FirstAsync(x => x.Name == AppRoles.Admin);
        var managerRole = await db.Roles.FirstAsync(x => x.Name == AppRoles.Manager);
        var staffRole = await db.Roles.FirstAsync(x => x.Name == AppRoles.Staff);
        var customerRole = await db.Roles.FirstAsync(x => x.Name == AppRoles.Customer);

        // ----- Categories -----
        if (await db.Categories.CountAsync() < Target)
        {
            var set = (await db.Categories.Select(x => x.Name).ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var need = Target - set.Count;
            var list = new List<Category>();
            for (var i = 1; list.Count < need && i < 100_000; i++)
            {
                var baseName = CategoryNames[(i - 1) % CategoryNames.Length];
                var name = Clip($"DM {baseName} {i:D4}", 100);
                if (!set.Add(name))
                    continue;

                list.Add(new Category
                {
                    Name = name,
                    Description = Clip($"Nhóm {baseName} — phân loại thuốc/vật tư kho nhà thuốc bệnh viện", 255),
                    IsActive = i % 17 != 0
                });
            }

            if (list.Count > 0)
            {
                db.Categories.AddRange(list);
                await db.SaveChangesAsync();
            }
        }

        // ----- Units -----
        if (await db.Units.CountAsync() < Target)
        {
            var unitDefs = PharmacyUnitDefs;

            var symbolSet = (await db.Units.Select(x => x.Symbol).ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var nameSet = (await db.Units.Select(x => x.Name).ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var need = Target - Math.Min(symbolSet.Count, nameSet.Count);
            // Count thật từ DB
            need = Target - await db.Units.CountAsync();
            var list = new List<Unit>();

            for (var i = 1; list.Count < need && i < 100_000; i++)
            {
                string name;
                string symbol;
                if (i <= unitDefs.Length)
                {
                    name = unitDefs[i - 1].Name;
                    symbol = unitDefs[i - 1].Symbol;
                }
                else
                {
                    name = $"Đơn vị đóng gói dược {i:D4}";
                    symbol = $"u{i:D4}";
                }

                if (!symbolSet.Add(symbol) || !nameSet.Add(name))
                    continue;

                list.Add(new Unit
                {
                    Name = Clip(name, 50),
                    Symbol = symbol,
                    Description = Clip($"Đơn vị đóng gói kho dược: {name}", 255),
                    IsActive = i % 11 != 0 || i <= unitDefs.Length
                });
            }

            if (list.Count > 0)
            {
                db.Units.AddRange(list);
                await db.SaveChangesAsync();
            }
        }

        // ----- Warehouses -----
        if (await db.Warehouses.CountAsync() < Target)
        {
            var codeSet = (await db.Warehouses.Select(x => x.Code).ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var nameSet = (await db.Warehouses.Select(x => x.Name).ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var need = Target - await db.Warehouses.CountAsync();
            var list = new List<Warehouse>();

            for (var i = 1; list.Count < need && i < 100_000; i++)
            {
                var code = $"WH{i:D4}";
                if (!codeSet.Add(code))
                    continue;

                var city = Cities[(i - 1) % Cities.Length];
                // Một số kho mang tên khu vực nghiệp vụ dược; phần còn lại gắn mã + thành phố.
                var name = i switch
                {
                    1 => $"Kho thuốc trung tâm BV {city}",
                    2 => $"Kho lạnh vắc xin / sinh phẩm {city}",
                    3 => $"Kho gây nghiện hướng thần {city}",
                    4 => $"Kho vật tư y tế tiêu hao {city}",
                    5 => $"Kho khoa dược lâm sàng {city}",
                    _ => $"Kho dược {city} {code}"
                };
                if (!nameSet.Add(name))
                    continue;

                var note = (i % 5) switch
                {
                    0 => "Kho lạnh 2-8°C (vắc xin, insulin)",
                    1 => "Kho thuốc gây nghiện / hướng thần — kiểm soát đặc biệt",
                    2 => "Kho vật tư vô trùng",
                    3 => "Kho cấp cứu 24/7",
                    _ => "Kho thuốc thường — nhiệt độ phòng"
                };

                list.Add(new Warehouse
                {
                    Code = code,
                    Name = name,
                    Address = $"{50 + i} {Streets[(i - 1) % Streets.Length]}, {city}",
                    Phone = $"09{(i % 100_000_000):D8}"[..10],
                    ManagerName = PersonName(i),
                    Note = note,
                    IsActive = i % 19 != 0
                });
            }

            if (list.Count > 0)
            {
                db.Warehouses.AddRange(list);
                await db.SaveChangesAsync();
            }
        }

        // ----- Suppliers -----
        if (await db.Suppliers.CountAsync() < Target)
        {
            var nameSet = (await db.Suppliers.Select(x => x.Name).ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var need = Target - nameSet.Count;
            var list = new List<Supplier>();
            for (var i = 1; list.Count < need && i < 100_000; i++)
            {
                var city = Cities[(i - 1) % Cities.Length];
                var baseSup = PharmaSupplierBases[(i - 1) % PharmaSupplierBases.Length];
                var name = Clip($"NCC {baseSup} · {city} #{i:D4}", 150);
                if (!nameSet.Add(name))
                    continue;

                list.Add(new Supplier
                {
                    Name = name,
                    ContactName = PersonName(i + 7),
                    Phone = $"08{(i % 100_000_000):D8}"[..10],
                    Email = $"ncc.duoc{i:D4}@pharma.vn",
                    Address = $"{100 + i} {Streets[i % Streets.Length]}, {city}",
                    TaxCode = $"1{100000000 + i}",
                    Note = "Nhà cung cấp thuốc / vật tư y tế cho bệnh viện",
                    IsActive = i % 13 != 0
                });
            }

            if (list.Count > 0)
            {
                db.Suppliers.AddRange(list);
                await db.SaveChangesAsync();
            }
        }

        // ----- Users (nhân sự kho + khách) -----
        var staffUsers = new List<User>();
        var customerUsers = new List<User>();

        var userCount = await db.Users.CountAsync();
        if (userCount < Target)
        {
            var need = Target - userCount;
            for (var i = 1; i <= need; i++)
            {
                var n = userCount + i;
                if (n <= 40)
                {
                    var roleId = n % 3 == 0 ? managerRole.Id : staffRole.Id;
                    var u = new User
                    {
                        Username = $"nv{n:D3}",
                        PasswordHash = PasswordHasher.HashPassword(DemoPassword),
                        FullName = PersonName(n),
                        Email = $"nv{n:D3}@bv-duoc.local",
                        Phone = $"0902{n:D6}"[..10],
                        RoleId = roleId,
                        IsActive = true
                    };
                    staffUsers.Add(u);
                }
                else
                {
                    var u = new User
                    {
                        Username = $"kh{n:D3}",
                        PasswordHash = PasswordHasher.HashPassword(DemoPassword),
                        FullName = PersonName(n + 3),
                        Email = $"kh{n:D3}@client.vn",
                        Phone = $"0903{n:D6}"[..10],
                        RoleId = customerRole.Id,
                        IsActive = true
                    };
                    customerUsers.Add(u);
                }
            }

            db.Users.AddRange(staffUsers.Concat(customerUsers));
            await db.SaveChangesAsync();
        }
        else
        {
            customerUsers = await db.Users
                .Where(x => x.RoleId == customerRole.Id && x.Username != "customer1")
                .Take(80)
                .ToListAsync();
        }

        // ----- Customers -----
        if (await db.Customers.CountAsync() < Target)
        {
            var linked = await db.Customers.Where(x => x.UserId != null).Select(x => x.UserId!.Value).ToListAsync();
            var linkedSet = linked.ToHashSet();
            var freeCustomerUsers = await db.Users
                .Where(x => x.RoleId == customerRole.Id && !linkedSet.Contains(x.Id))
                .OrderBy(x => x.Id)
                .Take(Target)
                .ToListAsync();

            var nameSet = (await db.Customers.Select(x => x.Name).ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var have = await db.Customers.CountAsync();
            var list = new List<Customer>();
            var userIdx = 0;

            for (var i = have + 1; i <= Target; i++)
            {
                var city = Cities[(i - 1) % Cities.Length];
                var dept = HospitalDepartments[(i - 1) % HospitalDepartments.Length];
                // "Khách hàng" trong ngữ cảnh bệnh viện = khoa/phòng nhận thuốc hoặc nhà thuốc vệ tinh.
                var name = Clip($"{dept} · BV {city} #{i:D4}", 150);
                if (!nameSet.Add(name))
                    continue;

                int? userId = null;
                if (userIdx < freeCustomerUsers.Count && i % 2 == 0)
                {
                    userId = freeCustomerUsers[userIdx].Id;
                    userIdx++;
                }

                list.Add(new Customer
                {
                    Name = name,
                    ContactName = PersonName(i + 11),
                    Phone = $"09{40 + (i % 50):D2}{(i % 1_000_000):D6}"[..10],
                    Email = $"khoa{i:D4}@bv-demo.vn",
                    Address = $"{200 + i} {Streets[i % Streets.Length]}, {city}",
                    TaxCode = $"2{200000000 + i}",
                    Note = "Khoa/phòng nhận thuốc hoặc nhà thuốc vệ tinh trong bệnh viện",
                    IsActive = i % 15 != 0,
                    UserId = userId
                });
            }

            if (list.Count > 0)
            {
                db.Customers.AddRange(list);
                await db.SaveChangesAsync();
            }
        }

        var categories = await db.Categories.OrderBy(x => x.Id).ToListAsync();
        var units = await db.Units.OrderBy(x => x.Id).ToListAsync();
        var warehouses = await db.Warehouses.OrderBy(x => x.Id).ToListAsync();
        var suppliers = await db.Suppliers.OrderBy(x => x.Id).ToListAsync();
        var customers = await db.Customers.OrderBy(x => x.Id).ToListAsync();

        // Products được EnsureDemoProductsAsync top-up sau SeedDemoDataAsync
        var productsAll = await db.Products.OrderBy(x => x.Id).ToListAsync();
        if (productsAll.Count == 0)
        {
            // Lần đầu: tạo tạm trong SeedDemoData để phiếu/tồn có FK — EnsureDemoProductsAsync sẽ bổ sung đủ Target
            await EnsureDemoProductsAsync(db);
            productsAll = await db.Products.OrderBy(x => x.Id).ToListAsync();
        }

        if (categories.Count == 0 || units.Count == 0 || warehouses.Count == 0
            || suppliers.Count == 0 || customers.Count == 0 || productsAll.Count == 0)
            return;

        // ----- WarehouseStocks (đủ Target dòng) + StockTransactions Manual -----
        var stockNeed = Target - await db.WarehouseStocks.CountAsync();
        if (stockNeed > 0)
        {
            var existingPairs = await db.WarehouseStocks
                .AsNoTracking()
                .Select(x => new { x.WarehouseId, x.ProductId })
                .ToListAsync();
            var pairSet = existingPairs
                .Select(x => (x.WarehouseId, x.ProductId))
                .ToHashSet();

            var stocks = new List<WarehouseStock>();
            var txs = new List<StockTransaction>();

            for (var w = 0; w < warehouses.Count && stocks.Count < stockNeed; w++)
            {
                for (var p = 0; p < productsAll.Count && stocks.Count < stockNeed; p++)
                {
                    var key = (warehouses[w].Id, productsAll[p].Id);
                    if (!pairSet.Add(key))
                        continue;

                    var qty = 20 + ((w + 1) * (p + 1) * 3) % 500;
                    stocks.Add(new WarehouseStock
                    {
                        WarehouseId = key.Item1,
                        ProductId = key.Item2,
                        Quantity = qty
                    });
                    txs.Add(new StockTransaction
                    {
                        WarehouseId = key.Item1,
                        ProductId = key.Item2,
                        Type = StockTransactionType.Manual,
                        QuantityChange = qty,
                        QuantityBefore = 0,
                        QuantityAfter = qty,
                        ReferenceType = StockReferenceTypes.WarehouseStock,
                        Note = "Seed tồn ban đầu"
                    });
                }
            }

            if (stocks.Count > 0)
            {
                db.WarehouseStocks.AddRange(stocks);
                db.StockTransactions.AddRange(txs);
                await db.SaveChangesAsync();
            }
        }

        // ----- ImportReceipts + Details (top-up PN-SEED-0001…0500) -----
        {
            var codeSet = (await db.ImportReceipts
                    .Where(x => x.Code.StartsWith("PN-SEED-"))
                    .Select(x => x.Code)
                    .ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var receipts = new List<ImportReceipt>();
            for (var i = 1; i <= Target; i++)
            {
                var code = $"PN-SEED-{i:D4}";
                if (!codeSet.Add(code))
                    continue;

                var status = i % 10 == 0
                    ? ImportReceiptStatus.Cancelled
                    : i % 3 == 0
                        ? ImportReceiptStatus.Draft
                        : ImportReceiptStatus.Confirmed;

                var wh = warehouses[(i - 1) % warehouses.Count];
                var sup = suppliers[(i - 1) % suppliers.Count];
                var p1 = productsAll[(i - 1) % productsAll.Count];

                // Phiếu nhập thực tế: nhiều dòng thuốc cùng lúc (2–4 sản phẩm).
                var details = new List<ImportReceiptDetail>();
                var lineCount = 2 + (i % 3);
                for (var d = 0; d < lineCount; d++)
                {
                    var p = productsAll[(i - 1 + d * 7) % productsAll.Count];
                    if (details.Any(x => x.ProductId == p.Id))
                        continue;
                    var qty = 10 + ((i + d) % 40);
                    var buyPrice = Math.Round(p.Price * 0.7m, 0); // giá nhập
                    var salePrice = Math.Round(p.Price, 0);        // giá bán (= trang người dùng)
                    details.Add(new ImportReceiptDetail
                    {
                        ProductId = p.Id,
                        Quantity = qty,
                        UnitPrice = buyPrice,
                        SalePrice = salePrice,
                        LineTotal = Math.Round(qty * buyPrice, 2),
                        Note = d == 0 ? "Dòng nhập chính" : null
                    });
                }

                if (details.Count == 0)
                {
                    var buy = Math.Round(p1.Price * 0.7m, 0);
                    details.Add(new ImportReceiptDetail
                    {
                        ProductId = p1.Id,
                        Quantity = 10,
                        UnitPrice = buy,
                        SalePrice = Math.Round(p1.Price, 0),
                        LineTotal = Math.Round(10 * buy, 2)
                    });
                }

                var goods = details.Sum(d => d.LineTotal);
                var shipping = 50_000m + (i % 7) * 10_000m;
                var discount = Math.Round(goods * ((i % 5) * 0.01m), 0);
                var tax = Math.Round(goods * 0.05m, 0); // thuế không hoàn lại demo 5%

                receipts.Add(new ImportReceipt
                {
                    Code = code,
                    ReceiptDate = DateTime.UtcNow.AddDays(-Target + i),
                    WarehouseId = wh.Id,
                    SupplierId = sup.Id,
                    Status = status,
                    Note = $"Nhập thuốc từ {Clip(sup.Name, 80)}",
                    ShippingFee = shipping,
                    TradeDiscount = discount,
                    NonRefundableTax = tax,
                    PurchaseInvoiceCode = status == ImportReceiptStatus.Confirmed ? $"HDN-SEED-{i:D4}" : null,
                    ConfirmedAt = status == ImportReceiptStatus.Confirmed ? DateTime.UtcNow.AddDays(-Target + i) : null,
                    CancelledAt = status == ImportReceiptStatus.Cancelled ? DateTime.UtcNow.AddDays(-Target + i + 1) : null,
                    Details = details
                });
            }

            if (receipts.Count > 0)
            {
                db.ImportReceipts.AddRange(receipts);
                await db.SaveChangesAsync();

                var newCodes = receipts.Select(x => x.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var confirmedImports = await db.ImportReceipts
                    .Include(x => x.Details)
                    .Where(x => x.Status == ImportReceiptStatus.Confirmed && newCodes.Contains(x.Code))
                    .ToListAsync();

                var importTxs = confirmedImports.SelectMany(r => r.Details.Select(d => new StockTransaction
                {
                    WarehouseId = r.WarehouseId,
                    ProductId = d.ProductId,
                    Type = StockTransactionType.Import,
                    QuantityChange = d.Quantity,
                    QuantityBefore = 0,
                    QuantityAfter = d.Quantity,
                    ReferenceType = StockReferenceTypes.ImportReceipt,
                    ReferenceId = r.Id,
                    ReferenceCode = r.Code,
                    Note = $"Seed nhap {r.Code}"
                })).ToList();

                if (importTxs.Count > 0)
                {
                    db.StockTransactions.AddRange(importTxs);
                    await db.SaveChangesAsync();
                }
            }
        }

        // ----- ExportReceipts + Details (top-up PX-SEED-0001…0500) -----
        {
            var codeSet = (await db.ExportReceipts
                    .Where(x => x.Code.StartsWith("PX-SEED-"))
                    .Select(x => x.Code)
                    .ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var receipts = new List<ExportReceipt>();
            for (var i = 1; i <= Target; i++)
            {
                var code = $"PX-SEED-{i:D4}";
                if (!codeSet.Add(code))
                    continue;

                var status = i % 10 == 0
                    ? ExportReceiptStatus.Cancelled
                    : i % 4 == 0
                        ? ExportReceiptStatus.Draft
                        : ExportReceiptStatus.Confirmed;

                var wh = warehouses[(i - 1) % warehouses.Count];
                var cus = customers[(i - 1) % customers.Count];

                // Xuất nhiều thuốc cùng phiếu (cấp phát khoa/phòng).
                var exportDetails = new List<ExportReceiptDetail>();
                var exportLines = 2 + (i % 3);
                for (var d = 0; d < exportLines; d++)
                {
                    var p = productsAll[(i + d * 11) % productsAll.Count];
                    if (exportDetails.Any(x => x.ProductId == p.Id))
                        continue;
                    var qty = 1 + ((i + d) % 10);
                    exportDetails.Add(new ExportReceiptDetail
                    {
                        ProductId = p.Id,
                        Quantity = qty,
                        UnitPrice = p.Price,
                        LineTotal = Math.Round(qty * p.Price, 2),
                        Note = d == 0 ? "Cấp phát theo yêu cầu khoa" : null
                    });
                }

                receipts.Add(new ExportReceipt
                {
                    Code = code,
                    ReceiptDate = DateTime.UtcNow.AddDays(-Target + i + 2),
                    WarehouseId = wh.Id,
                    CustomerId = cus.Id,
                    Status = status,
                    Note = $"Xuất cấp phát cho {Clip(cus.Name, 80)}",
                    ConfirmedAt = status == ExportReceiptStatus.Confirmed ? DateTime.UtcNow.AddDays(-Target + i + 2) : null,
                    CancelledAt = status == ExportReceiptStatus.Cancelled ? DateTime.UtcNow.AddDays(-Target + i + 3) : null,
                    Details = exportDetails
                });
            }

            if (receipts.Count > 0)
            {
                db.ExportReceipts.AddRange(receipts);
                await db.SaveChangesAsync();

                var newCodes = receipts.Select(x => x.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var confirmedExports = await db.ExportReceipts
                    .Include(x => x.Details)
                    .Where(x => x.Status == ExportReceiptStatus.Confirmed && newCodes.Contains(x.Code))
                    .ToListAsync();

                var exportTxs = confirmedExports.SelectMany(r => r.Details.Select(d => new StockTransaction
                {
                    WarehouseId = r.WarehouseId,
                    ProductId = d.ProductId,
                    Type = StockTransactionType.Export,
                    QuantityChange = -d.Quantity,
                    QuantityBefore = d.Quantity,
                    QuantityAfter = 0,
                    ReferenceType = StockReferenceTypes.ExportReceipt,
                    ReferenceId = r.Id,
                    ReferenceCode = r.Code,
                    Note = $"Seed xuat {r.Code}"
                })).ToList();

                if (exportTxs.Count > 0)
                {
                    db.StockTransactions.AddRange(exportTxs);
                    await db.SaveChangesAsync();
                }
            }
        }

        // ----- Stocktakes + Details (top-up KK-SEED-0001…0500) -----
        {
            var codeSet = (await db.Stocktakes
                    .Where(x => x.Code.StartsWith("KK-SEED-"))
                    .Select(x => x.Code)
                    .ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var list = new List<Stocktake>();
            for (var i = 1; i <= Target; i++)
            {
                var code = $"KK-SEED-{i:D4}";
                if (!codeSet.Add(code))
                    continue;

                var status = i % 5 == 0
                    ? StocktakeStatus.Cancelled
                    : i % 3 == 0
                        ? StocktakeStatus.Draft
                        : StocktakeStatus.Confirmed;

                var wh = warehouses[(i - 1) % warehouses.Count];
                var p1 = productsAll[(i - 1) % productsAll.Count];
                var systemQty = 50 + (i % 30);
                var counted = systemQty + (i % 7) - 3;

                list.Add(new Stocktake
                {
                    Code = code,
                    StocktakeDate = DateTime.UtcNow.AddDays(-i),
                    WarehouseId = wh.Id,
                    Status = status,
                    Note = $"Kiem ke dinh ky kho {wh.Code}",
                    ConfirmedAt = status == StocktakeStatus.Confirmed ? DateTime.UtcNow.AddDays(-i) : null,
                    CancelledAt = status == StocktakeStatus.Cancelled ? DateTime.UtcNow.AddDays(-i + 1) : null,
                    Details =
                    [
                        new StocktakeDetail
                        {
                            ProductId = p1.Id,
                            SystemQuantity = systemQty,
                            CountedQuantity = counted,
                            Difference = counted - systemQty,
                            Note = counted == systemQty ? "Khop" : "Co chenh lech"
                        }
                    ]
                });
            }

            if (list.Count > 0)
            {
                db.Stocktakes.AddRange(list);
                await db.SaveChangesAsync();

                var newCodes = list.Select(x => x.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var confirmedKk = await db.Stocktakes
                    .Include(x => x.Details)
                    .Where(x => x.Status == StocktakeStatus.Confirmed && newCodes.Contains(x.Code))
                    .ToListAsync();

                var kkTxs = confirmedKk.SelectMany(s => s.Details.Select(d => new StockTransaction
                {
                    WarehouseId = s.WarehouseId,
                    ProductId = d.ProductId,
                    Type = StockTransactionType.Stocktake,
                    QuantityChange = d.Difference,
                    QuantityBefore = d.SystemQuantity,
                    QuantityAfter = d.CountedQuantity,
                    ReferenceType = StockReferenceTypes.Stocktake,
                    ReferenceId = s.Id,
                    ReferenceCode = s.Code,
                    Note = $"Seed kiem ke {s.Code}"
                })).ToList();

                if (kkTxs.Count > 0)
                {
                    db.StockTransactions.AddRange(kkTxs);
                    await db.SaveChangesAsync();
                }
            }
        }
    }

    /// <summary>
    /// Ghép họ + tên đệm + tên từ seed số (ổn định, tái lập được).
    /// </summary>
    /// <summary>
    /// Gán thuộc tính dược BV (SĐK, hoạt chất, dạng bào chế, kiểm soát, lạnh…).
    /// </summary>
    private static void ApplyPharmacyAttrs(Product product, string drugName, int idx)
    {
        var forms = new[] { "Viên nén", "Viên nang", "Ống tiêm", "Lọ tiêm", "Gói bột", "Dịch truyền", "Kem bôi", "Siro" };
        var makers = new[]
        {
            "DHG Pharma", "Traphaco", "Imexpharm", "Pymepharco", "Stada Việt Nam",
            "Sanofi", "Pfizer", "GSK", "AstraZeneca", "Roche"
        };
        var controlledHints = new[]
        {
            "morphin", "pethidin", "fentanyl", "codein", "diazepam", "alprazolam",
            "lorazepam", "clonazepam", "midazolam", "ketamin", "tramadol"
        };
        var coldHints = new[] { "insulin", "vắc", "vaccine", "albumin", "propofol", "octreotid" };

        var lower = drugName.ToLowerInvariant();
        product.RegistrationNumber = $"VD-{20000 + (idx % 8000)}-{25 + (idx % 5)}";
        product.ActiveIngredient = Clip(drugName.Split('(')[0].Trim(), 200);
        product.Strength = (idx % 5) switch
        {
            0 => "500mg",
            1 => "250mg",
            2 => "5mg/ml",
            3 => "10mg",
            _ => "1g"
        };
        product.DosageForm = forms[idx % forms.Length];
        product.Manufacturer = makers[idx % makers.Length];
        product.IsControlledSubstance = controlledHints.Any(h => lower.Contains(h));
        product.RequiresColdStorage = coldHints.Any(h => lower.Contains(h));
        product.ExpiryAlertDays = product.RequiresColdStorage ? 60 : 90;
    }

    private static string Clip(string value, int maxLen)
        => string.IsNullOrEmpty(value) || value.Length <= maxLen ? value : value[..maxLen];

    private static string PersonName(int seed)
    {
        var ln = LastNames[Math.Abs(seed) % LastNames.Length];
        var mn = MidNames[Math.Abs(seed * 3) % MidNames.Length];
        var fn = FirstNames[Math.Abs(seed * 7) % FirstNames.Length];
        return $"{ln} {mn} {fn}";
    }

    /// <summary>
    /// Tạo user nếu chưa có username; trả về user hiện có hoặc mới tạo.
    /// </summary>
    private static async Task<User> EnsureUserAsync(
        ApplicationDbContext db,
        string username,
        string password,
        string fullName,
        string email,
        int roleId,
        string? phone = null)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Username == username);
        if (user is not null)
        {
            // Đồng bộ hồ sơ demo sang ngữ cảnh kho dược (không đổi username/password).
            user.FullName = fullName;
            user.Email = email;
            user.Phone = phone;
            user.RoleId = roleId;
            user.IsActive = true;
            await db.SaveChangesAsync();
            return user;
        }

        user = new User
        {
            Username = username,
            PasswordHash = PasswordHasher.HashPassword(password),
            FullName = fullName,
            Email = email,
            Phone = phone,
            RoleId = roleId,
            IsActive = true
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }
}
