using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Infrastructure.Persistence;

/// <summary>
/// Seed role/user đăng nhập + ~100 bản ghi mẫu thực tế cho các bảng nghiệp vụ.
/// </summary>
public static class DataSeeder
{
    private const int Target = 100; // Số bản ghi mục tiêu mỗi bảng demo
    private const string DemoPassword = "123456"; // Mật khẩu demo cho mọi user seed

    // ----- Dữ liệu mẫu tên/địa chỉ/danh mục (dùng quay vòng khi sinh bản ghi) -----
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

    private static readonly string[] CategoryNames =
    [
        "Thực phẩm khô", "Thực phẩm tươi", "Đồ uống", "Sữa và chế phẩm", "Bánh kẹo",
        "Gia vị", "Đồ hộp", "Hải sản đông lạnh", "Thịt đông lạnh", "Rau củ quả",
        "Ngũ cốc", "Dầu ăn", "Đồ dùng nhà bếp", "Vệ sinh cá nhân", "Hóa mỹ phẩm",
        "Đồ giấy", "Văn phòng phẩm", "Thiết bị điện nhỏ", "Phụ kiện điện thoại", "Đồ chơi",
        "Thời trang", "Giày dép", "Túi xách", "Dược phẩm OTC", "Thực phẩm chức năng",
        "Thức ăn chăn nuôi", "Vật liệu xây dựng nhẹ", "Sơn và keo", "Dụng cụ cầm tay",
        "Bao bì đóng gói"
    ];

    private static readonly string[] ProductBases =
    [
        "Gạo ST25", "Gạo Jasmine", "Đường trắng", "Muối i-ốt", "Nước mắm", "Dầu đậu nành",
        "Mì gói", "Phở khô", "Sữa tươi", "Sữa đặc", "Cà phê rang xay", "Trà xanh",
        "Nước suối", "Nước ngọt", "Bia lon", "Khăn giấy", "Nước rửa chén", "Nước giặt",
        "Bột giặt", "Xà phòng", "Dầu gội", "Sữa tắm", "Kem đánh răng", "Bàn chải",
        "Pin AA", "Bóng đèn LED", "Ổ cắm điện", "Băng keo", "Thùng carton", "Màng PE"
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
        await EnsureProductImageFilesAsync(db, env.ContentRootPath);
    }

    /// <summary>
    /// Tạo file placeholder cho mọi ImageUrl đang thiếu trên đĩa
    /// (tránh 404 / ảnh vỡ trên UI admin & store).
    /// </summary>
    private static async Task EnsureProductImageFilesAsync(ApplicationDbContext db, string contentRoot)
    {
        var webRoot = Path.Combine(contentRoot, "wwwroot");
        var productDir = Path.Combine(webRoot, "uploads", "products");
        Directory.CreateDirectory(productDir);

        var urls = await db.ProductImages
            .AsNoTracking()
            .Select(x => x.ImageUrl)
            .Distinct()
            .ToListAsync();

        // PNG 1x1 xám hợp lệ
        var placeholder = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

        foreach (var url in urls)
        {
            if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
                continue;

            var fullPath = Path.Combine(
                webRoot,
                url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            if (!File.Exists(fullPath))
                await File.WriteAllBytesAsync(fullPath, placeholder);
        }
    }

    // -------------------------------------------------------------------------
    // Roles + tài khoản đăng nhập cố định (admin / manager / staff / customer1)
    // -------------------------------------------------------------------------
    /// <summary>
    /// Tạo 4 role hệ thống và các user đăng nhập demo (kèm 1 Customer gắn customer1).
    /// </summary>
    private static async Task SeedRolesAndLoginUsersAsync(ApplicationDbContext db)
    {
        var roles = new (string Name, string Description)[]
        {
            (AppRoles.Admin, "Quản trị toàn bộ hệ thống"),
            (AppRoles.Manager, "Quản lý kho"),
            (AppRoles.Staff, "Nhân viên kho"),
            (AppRoles.Customer, "Tài khoản khách hàng")
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

        await EnsureUserAsync(db, "admin", DemoPassword, "Nguyễn Quản Trị", "admin@warehouse.local", adminRole.Id, "0901000001");
        await EnsureUserAsync(db, "manager", DemoPassword, "Trần Quản Lý Kho", "manager@warehouse.local", managerRole.Id, "0901000002");
        await EnsureUserAsync(db, "staff", DemoPassword, "Lê Nhân Viên Kho", "staff@warehouse.local", staffRole.Id, "0901000003");

        var customerUser = await EnsureUserAsync(
            db, "customer1", DemoPassword, "Phạm Khách Hàng", "customer1@email.com", customerRole.Id, "0909000111");

        if (!await db.Customers.AnyAsync(x => x.UserId == customerUser.Id))
        {
            db.Customers.Add(new Customer
            {
                Name = "Công ty TNHH Demo Retail",
                ContactName = "Phạm Khách Hàng",
                Email = "customer1@email.com",
                Phone = "0909000111",
                Address = "123 Nguyễn Trãi, Thanh Xuân, Hà Nội",
                TaxCode = "0101234567",
                IsActive = true,
                UserId = customerUser.Id
            });
            await db.SaveChangesAsync();
        }
    }

    // -------------------------------------------------------------------------
    // ~100 bản ghi / bảng nghiệp vụ (chỉ chạy 1 lần — mốc PN-SEED-0100)
    // -------------------------------------------------------------------------
    /// <summary>
    /// Sinh dữ liệu demo Categories → Units → Warehouses → Suppliers → Users →
    /// Customers → Products → Stocks → Import/Export/Stocktake + StockTransactions.
    /// </summary>
    private static async Task SeedDemoDataAsync(ApplicationDbContext db)
    {
        // Đã seed đủ bộ demo (dùng mã phiếu nhập cuối làm mốc)
        if (await db.ImportReceipts.AnyAsync(x => x.Code == "PN-SEED-0100"))
            return;

        var adminRole = await db.Roles.FirstAsync(x => x.Name == AppRoles.Admin);
        var managerRole = await db.Roles.FirstAsync(x => x.Name == AppRoles.Manager);
        var staffRole = await db.Roles.FirstAsync(x => x.Name == AppRoles.Staff);
        var customerRole = await db.Roles.FirstAsync(x => x.Name == AppRoles.Customer);

        // ----- Categories -----
        if (await db.Categories.CountAsync() < Target)
        {
            var existing = await db.Categories.Select(x => x.Name).ToListAsync();
            var set = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var list = new List<Category>();

            for (var i = 1; i <= Target; i++)
            {
                var baseName = CategoryNames[(i - 1) % CategoryNames.Length];
                var name = i <= CategoryNames.Length
                    ? baseName
                    : $"{baseName} nhóm {(i - 1) / CategoryNames.Length + 1}";

                if (!set.Add(name))
                    name = $"{baseName} #{i:D3}";

                list.Add(new Category
                {
                    Name = name,
                    Description = $"Danh mục {name} phục vụ quản lý kho phân phối",
                    IsActive = i % 17 != 0
                });
            }

            db.Categories.AddRange(list);
            await db.SaveChangesAsync();
        }

        // ----- Units -----
        if (await db.Units.CountAsync() < Target)
        {
            var unitDefs = new (string Name, string Symbol)[]
            {
                ("Kilogram", "kg"), ("Gram", "g"), ("Tấn", "tan"), ("Yến", "yen"),
                ("Lít", "l"), ("Mililít", "ml"), ("Mét", "m"), ("Centimet", "cm"),
                ("Cái", "cai"), ("Chiếc", "chiec"), ("Bộ", "bo"), ("Hộp", "hop"),
                ("Thùng", "thung"), ("Chai", "chai"), ("Lon", "lon"), ("Bao", "bao"),
                ("Gói", "goi"), ("Vỉ", "vi"), ("Cuộn", "cuon"), ("Tờ", "to"),
                ("Đôi", "doi"), ("Bình", "binh"), ("Can", "can"), ("Thùng 12", "th12"),
                ("Thùng 24", "th24"), ("Lốc 6", "loc6"), ("Lốc 10", "loc10")
            };

            var list = new List<Unit>();
            for (var i = 1; i <= Target; i++)
            {
                if (i <= unitDefs.Length)
                {
                    list.Add(new Unit
                    {
                        Name = unitDefs[i - 1].Name,
                        Symbol = unitDefs[i - 1].Symbol,
                        Description = $"Đơn vị {unitDefs[i - 1].Name}",
                        IsActive = true
                    });
                }
                else
                {
                    list.Add(new Unit
                    {
                        Name = $"Đơn vị đóng gói {i:D3}",
                        Symbol = $"u{i:D3}",
                        Description = "Đơn vị quy đổi nội bộ kho",
                        IsActive = i % 11 != 0
                    });
                }
            }

            // tránh trùng nếu đã có vài unit
            var existSymbols = await db.Units.Select(x => x.Symbol).ToListAsync();
            var symbolSet = existSymbols.ToHashSet(StringComparer.OrdinalIgnoreCase);
            db.Units.AddRange(list.Where(x => symbolSet.Add(x.Symbol)));
            await db.SaveChangesAsync();
        }

        // ----- Warehouses -----
        if (await db.Warehouses.CountAsync() < Target)
        {
            var list = new List<Warehouse>();
            for (var i = 1; i <= Target; i++)
            {
                var city = Cities[(i - 1) % Cities.Length];
                list.Add(new Warehouse
                {
                    Code = $"WH{i:D3}",
                    Name = $"Kho {city} {(i > Cities.Length ? $"#{i}" : "trung tâm").Trim()}",
                    Address = $"{50 + i} {Streets[(i - 1) % Streets.Length]}, {city}",
                    Phone = $"09{i:D8}"[..10],
                    ManagerName = PersonName(i),
                    Note = i % 5 == 0 ? "Kho lạnh" : "Kho thường",
                    IsActive = i % 19 != 0
                });
            }

            // sửa tên trùng logic
            for (var i = 0; i < list.Count; i++)
            {
                list[i].Name = $"Kho {Cities[i % Cities.Length]} {list[i].Code}";
            }

            db.Warehouses.AddRange(list);
            await db.SaveChangesAsync();
        }

        // ----- Suppliers -----
        if (await db.Suppliers.CountAsync() < Target)
        {
            var list = new List<Supplier>();
            for (var i = 1; i <= Target; i++)
            {
                var city = Cities[(i - 1) % Cities.Length];
                list.Add(new Supplier
                {
                    Name = $"Công ty CP Cung ứng {city} {i:D3}",
                    ContactName = PersonName(i + 7),
                    Phone = $"08{i:D8}"[..10],
                    Email = $"ncc{i:D3}@supply.vn",
                    Address = $"{100 + i} {Streets[i % Streets.Length]}, {city}",
                    TaxCode = $"0{100000000 + i}",
                    Note = "Nhà cung cấp phân phối hàng tiêu dùng",
                    IsActive = i % 13 != 0
                });
            }

            db.Suppliers.AddRange(list);
            await db.SaveChangesAsync();
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
                        Email = $"nv{n:D3}@warehouse.local",
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
                .Take(80)
                .ToListAsync();

            var have = await db.Customers.CountAsync();
            var list = new List<Customer>();
            var userIdx = 0;

            for (var i = have + 1; i <= Target; i++)
            {
                var city = Cities[(i - 1) % Cities.Length];
                int? userId = null;
                if (userIdx < freeCustomerUsers.Count && i % 2 == 0)
                {
                    userId = freeCustomerUsers[userIdx].Id;
                    userIdx++;
                }

                list.Add(new Customer
                {
                    Name = $"Siêu thị / Đại lý {city} {i:D3}",
                    ContactName = PersonName(i + 11),
                    Phone = $"09{40 + (i % 50):D2}{i:D6}"[..10],
                    Email = $"daily{i:D3}@retail.vn",
                    Address = $"{200 + i} {Streets[i % Streets.Length]}, {city}",
                    TaxCode = $"0{200000000 + i}",
                    Note = "Khách hàng bán lẻ / phân phối",
                    IsActive = i % 15 != 0,
                    UserId = userId
                });
            }

            db.Customers.AddRange(list);
            await db.SaveChangesAsync();
        }

        var categories = await db.Categories.OrderBy(x => x.Id).ToListAsync();
        var units = await db.Units.OrderBy(x => x.Id).ToListAsync();
        var warehouses = await db.Warehouses.OrderBy(x => x.Id).ToListAsync();
        var suppliers = await db.Suppliers.OrderBy(x => x.Id).ToListAsync();
        var customers = await db.Customers.OrderBy(x => x.Id).ToListAsync();

        // ----- Products + ProductImages -----
        if (await db.Products.CountAsync() < Target)
        {
            var products = new List<Product>();
            for (var i = 1; i <= Target; i++)
            {
                var baseName = ProductBases[(i - 1) % ProductBases.Length];
                var name = i <= ProductBases.Length
                    ? baseName
                    : $"{baseName} loại {(char)('A' + ((i - 1) / ProductBases.Length) % 26)}";

                products.Add(new Product
                {
                    Sku = $"SP{i:D4}",
                    Name = name,
                    Description = $"{name} — hàng tiêu dùng phổ biến, đóng gói sẵn xuất kho",
                    Price = 5000 + (i * 137) % 500000,
                    MinStock = 5 + (i % 20),
                    CategoryId = categories[(i - 1) % categories.Count].Id,
                    UnitId = units[(i - 1) % units.Count].Id,
                    IsActive = i % 21 != 0
                });
            }

            db.Products.AddRange(products);
            await db.SaveChangesAsync();

            // ~100+ ảnh (1-2 ảnh / sản phẩm đầu)
            var images = new List<ProductImage>();
            var savedProducts = await db.Products.OrderBy(x => x.Id).Take(Target).ToListAsync();
            var imgId = 0;
            foreach (var p in savedProducts)
            {
                imgId++;
                images.Add(new ProductImage
                {
                    ProductId = p.Id,
                    ImageUrl = $"/uploads/products/demo-{p.Sku.ToLowerInvariant()}-1.png",
                    SortOrder = 0,
                    IsPrimary = true
                });

                if (imgId <= 50)
                {
                    images.Add(new ProductImage
                    {
                        ProductId = p.Id,
                        ImageUrl = $"/uploads/products/demo-{p.Sku.ToLowerInvariant()}-2.png",
                        SortOrder = 1,
                        IsPrimary = false
                    });
                }
            }

            db.ProductImages.AddRange(images);
            await db.SaveChangesAsync();
        }

        var productsAll = await db.Products.OrderBy(x => x.Id).ToListAsync();

        // ----- WarehouseStocks (đủ 100 dòng) + StockTransactions Manual -----
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

        // ----- ImportReceipts + Details (100) -----
        if (!await db.ImportReceipts.AnyAsync(x => x.Code.StartsWith("PN-SEED-")))
        {
            var receipts = new List<ImportReceipt>();
            for (var i = 1; i <= Target; i++)
            {
                var status = i % 10 == 0
                    ? ImportReceiptStatus.Cancelled
                    : i % 3 == 0
                        ? ImportReceiptStatus.Draft
                        : ImportReceiptStatus.Confirmed;

                var wh = warehouses[(i - 1) % warehouses.Count];
                var sup = suppliers[(i - 1) % suppliers.Count];
                var p1 = productsAll[(i - 1) % productsAll.Count];
                var p2 = productsAll[i % productsAll.Count];

                var details = new List<ImportReceiptDetail>
                {
                    new()
                    {
                        ProductId = p1.Id,
                        Quantity = 10 + (i % 40),
                        UnitPrice = p1.Price * 0.7m,
                        LineTotal = Math.Round((10 + (i % 40)) * p1.Price * 0.7m, 2),
                        Note = "Dòng nhập chính"
                    }
                };

                if (p2.Id != p1.Id && i % 2 == 0)
                {
                    details.Add(new ImportReceiptDetail
                    {
                        ProductId = p2.Id,
                        Quantity = 5 + (i % 15),
                        UnitPrice = p2.Price * 0.75m,
                        LineTotal = Math.Round((5 + (i % 15)) * p2.Price * 0.75m, 2)
                    });
                }

                receipts.Add(new ImportReceipt
                {
                    Code = $"PN-SEED-{i:D4}",
                    ReceiptDate = DateTime.UtcNow.AddDays(-Target + i),
                    WarehouseId = wh.Id,
                    SupplierId = sup.Id,
                    Status = status,
                    Note = $"Phiếu nhập hàng từ {sup.Name}",
                    ConfirmedAt = status == ImportReceiptStatus.Confirmed ? DateTime.UtcNow.AddDays(-Target + i) : null,
                    CancelledAt = status == ImportReceiptStatus.Cancelled ? DateTime.UtcNow.AddDays(-Target + i + 1) : null,
                    Details = details
                });
            }

            db.ImportReceipts.AddRange(receipts);
            await db.SaveChangesAsync();

            // Ghi transaction minh họa cho phiếu Confirmed (không đụng lại tồn seed để tránh lệch lớn)
            var confirmedImports = await db.ImportReceipts
                .Include(x => x.Details)
                .Where(x => x.Status == ImportReceiptStatus.Confirmed && x.Code.StartsWith("PN-SEED-"))
                .ToListAsync();

            var importTxs = new List<StockTransaction>();
            foreach (var r in confirmedImports)
            {
                foreach (var d in r.Details)
                {
                    importTxs.Add(new StockTransaction
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
                        Note = $"Seed nhập {r.Code}"
                    });
                }
            }

            db.StockTransactions.AddRange(importTxs);
            await db.SaveChangesAsync();
        }

        // ----- ExportReceipts + Details (100) -----
        if (!await db.ExportReceipts.AnyAsync(x => x.Code.StartsWith("PX-SEED-")))
        {
            var receipts = new List<ExportReceipt>();
            for (var i = 1; i <= Target; i++)
            {
                var status = i % 10 == 0
                    ? ExportReceiptStatus.Cancelled
                    : i % 4 == 0
                        ? ExportReceiptStatus.Draft
                        : ExportReceiptStatus.Confirmed;

                var wh = warehouses[(i - 1) % warehouses.Count];
                var cus = customers[(i - 1) % customers.Count];
                var p1 = productsAll[(i - 1) % productsAll.Count];

                receipts.Add(new ExportReceipt
                {
                    Code = $"PX-SEED-{i:D4}",
                    ReceiptDate = DateTime.UtcNow.AddDays(-Target + i + 2),
                    WarehouseId = wh.Id,
                    CustomerId = cus.Id,
                    Status = status,
                    Note = $"Xuất bán cho {cus.Name}",
                    ConfirmedAt = status == ExportReceiptStatus.Confirmed ? DateTime.UtcNow.AddDays(-Target + i + 2) : null,
                    CancelledAt = status == ExportReceiptStatus.Cancelled ? DateTime.UtcNow.AddDays(-Target + i + 3) : null,
                    Details =
                    [
                        new ExportReceiptDetail
                        {
                            ProductId = p1.Id,
                            Quantity = 1 + (i % 10),
                            UnitPrice = p1.Price,
                            LineTotal = Math.Round((1 + (i % 10)) * p1.Price, 2),
                            Note = "Xuất theo đơn"
                        }
                    ]
                });
            }

            db.ExportReceipts.AddRange(receipts);
            await db.SaveChangesAsync();

            var confirmedExports = await db.ExportReceipts
                .Include(x => x.Details)
                .Where(x => x.Status == ExportReceiptStatus.Confirmed && x.Code.StartsWith("PX-SEED-"))
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
                Note = $"Seed xuất {r.Code}"
            })).ToList();

            db.StockTransactions.AddRange(exportTxs);
            await db.SaveChangesAsync();
        }

        // ----- Stocktakes + Details (100) -----
        if (!await db.Stocktakes.AnyAsync(x => x.Code.StartsWith("KK-SEED-")))
        {
            var list = new List<Stocktake>();
            for (var i = 1; i <= Target; i++)
            {
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
                    Code = $"KK-SEED-{i:D4}",
                    StocktakeDate = DateTime.UtcNow.AddDays(-i),
                    WarehouseId = wh.Id,
                    Status = status,
                    Note = $"Kiểm kê định kỳ kho {wh.Code}",
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
                            Note = counted == systemQty ? "Khớp" : "Có chênh lệch"
                        }
                    ]
                });
            }

            db.Stocktakes.AddRange(list);
            await db.SaveChangesAsync();

            var confirmedKk = await db.Stocktakes
                .Include(x => x.Details)
                .Where(x => x.Status == StocktakeStatus.Confirmed && x.Code.StartsWith("KK-SEED-"))
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
                Note = $"Seed kiểm kê {s.Code}"
            })).ToList();

            db.StockTransactions.AddRange(kkTxs);
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Ghép họ + tên đệm + tên từ seed số (ổn định, tái lập được).
    /// </summary>
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
            return user;

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
