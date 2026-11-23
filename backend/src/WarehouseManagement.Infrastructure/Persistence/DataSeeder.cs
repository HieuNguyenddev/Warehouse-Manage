using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WarehouseManagement.Domain;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Infrastructure.Persistence;

/// <summary>
/// Seed role/user đăng nhập + dữ liệu demo nghiệp vụ.
/// Mỗi bảng nghiệp vụ: <see cref="Target"/> bản ghi (mặc định 500) để test phân trang / filter.
/// </summary>
public static class DataSeeder
{
    private const int Target = 500; // Số bản ghi mục tiêu mỗi bảng demo
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
        "Pin AA", "Bóng đèn LED", "Ổ cắm điện", "Băng keo", "Thùng carton", "Màng PE",
        "Bánh quy", "Kẹo mềm", "Snack khoai", "Hạt điều", "Nho khô", "Tương ớt",
        "Tương cà", "Mayonnaise", "Dầu oliu", "Bơ thực vật", "Phô mai", "Yaourt",
        "Thịt heo đông lạnh", "Thịt gà đông lạnh", "Cá basa", "Tôm sú", "Rau củ hỗn hợp",
        "Nấm đông cô", "Đậu hũ", "Trứng gà", "Mật ong", "Yến mạch"
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
        // Top-up Products → Target (idempotent) + ảnh hiển thị được trên UI
        await EnsureDemoProductsAsync(db);
        await EnsureProductImagesReadyAsync(db, env.ContentRootPath);
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

        var products = new List<Product>();
        for (var i = 1; i <= Target; i++)
        {
            var sku = $"SP{i:D4}";
            if (existingSkus.Contains(sku))
                continue;

            var baseName = ProductBases[(i - 1) % ProductBases.Length];
            var batch = (i - 1) / ProductBases.Length;
            var name = batch == 0 ? baseName : $"{baseName} lo {batch + 1}";

            products.Add(new Product
            {
                Sku = sku,
                Name = name,
                Description = $"{name} - hang demo tieu dung (SKU {sku})",
                Price = 5000 + (i * 137) % 500000,
                MinStock = 5 + (i % 20),
                CategoryId = categories[(i - 1) % categories.Count].Id,
                UnitId = units[(i - 1) % units.Count].Id,
                IsActive = i % 21 != 0
            });
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
                ImageUrl = $"/uploads/products/demo-{p.Sku.ToLowerInvariant()}-1.png",
                SortOrder = 0,
                IsPrimary = true
            });

            if (n <= 50)
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

    /// <summary>
    /// Đảm bảo mọi Product có ≥1 ảnh primary trong DB và file trên đĩa là PNG demo nhìn thấy được
    /// (thay placeholder 1×1 trước đây khiến UI hiện ô trống / vỡ ảnh).
    /// Template: wwwroot/uploads/products/templates/pack-00.png … pack-23.png
    /// </summary>
    private static async Task EnsureProductImagesReadyAsync(ApplicationDbContext db, string contentRoot)
    {
        var webRoot = Path.Combine(contentRoot, "wwwroot");
        var productDir = Path.Combine(webRoot, "uploads", "products");
        var templateDir = Path.Combine(productDir, "templates");
        Directory.CreateDirectory(productDir);
        Directory.CreateDirectory(templateDir);

        var templates = Directory.Exists(templateDir)
            ? Directory.GetFiles(templateDir, "pack-*.png").OrderBy(x => x, StringComparer.Ordinal).ToArray()
            : [];

        // ----- 1) DB: mọi sản phẩm có ít nhất 1 ảnh primary -----
        var products = await db.Products
            .Include(x => x.Images)
            .OrderBy(x => x.Id)
            .ToListAsync();

        var dbChanged = false;
        foreach (var p in products)
        {
            if (p.Images.Count == 0)
            {
                p.Images.Add(new ProductImage
                {
                    ImageUrl = $"/uploads/products/demo-{p.Sku.ToLowerInvariant()}-1.png",
                    SortOrder = 0,
                    IsPrimary = true
                });
                dbChanged = true;
                continue;
            }

            if (!p.Images.Any(x => x.IsPrimary))
            {
                p.Images.OrderBy(x => x.SortOrder).ThenBy(x => x.Id).First().IsPrimary = true;
                dbChanged = true;
            }
        }

        if (dbChanged)
            await db.SaveChangesAsync();

        // ----- 2) Đĩa: tạo/ghi đè file ảnh demo (thiếu hoặc chỉ còn PNG 1×1) -----
        var urls = await db.ProductImages
            .AsNoTracking()
            .Select(x => x.ImageUrl)
            .Distinct()
            .ToListAsync();

        // Fallback PNG nhỏ nếu thiếu template (hiếm)
        var tinyFallback = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

        for (var i = 0; i < urls.Count; i++)
        {
            var url = urls[i];
            if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
                continue;

            // Không đụng template gốc
            if (url.Contains("/templates/", StringComparison.OrdinalIgnoreCase))
                continue;

            var fullPath = Path.Combine(
                webRoot,
                url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var missingOrTiny = !File.Exists(fullPath) || new FileInfo(fullPath).Length < 500;
            if (!missingOrTiny)
                continue;

            if (templates.Length > 0)
            {
                var idx = Math.Abs(HashCode.Combine(url.ToLowerInvariant())) % templates.Length;
                File.Copy(templates[idx], fullPath, overwrite: true);
            }
            else
            {
                await File.WriteAllBytesAsync(fullPath, tinyFallback);
            }
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
    // Target bản ghi / bảng nghiệp vụ — idempotent top-up (có thể chạy lại)
    // -------------------------------------------------------------------------
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
                var name = $"DM {baseName} {i:D4}";
                if (!set.Add(name))
                    continue;

                list.Add(new Category
                {
                    Name = name,
                    Description = $"Danh mục {name} phục vụ quản lý kho phân phối",
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
                    name = $"Don vi dong goi {i:D4}";
                    symbol = $"u{i:D4}";
                }

                if (!symbolSet.Add(symbol) || !nameSet.Add(name))
                    continue;

                list.Add(new Unit
                {
                    Name = name,
                    Symbol = symbol,
                    Description = $"Đơn vị {name}",
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
                var name = $"Kho demo {city} {code}";
                if (!nameSet.Add(name))
                    continue;

                list.Add(new Warehouse
                {
                    Code = code,
                    Name = name,
                    Address = $"{50 + i} {Streets[(i - 1) % Streets.Length]}, {city}",
                    Phone = $"09{(i % 100_000_000):D8}"[..10],
                    ManagerName = PersonName(i),
                    Note = i % 5 == 0 ? "Kho lạnh" : "Kho thường",
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
                var name = $"NCC demo {city} {i:D4}";
                if (!nameSet.Add(name))
                    continue;

                list.Add(new Supplier
                {
                    Name = name,
                    ContactName = PersonName(i + 7),
                    Phone = $"08{(i % 100_000_000):D8}"[..10],
                    Email = $"nccdemo{i:D4}@supply.vn",
                    Address = $"{100 + i} {Streets[i % Streets.Length]}, {city}",
                    TaxCode = $"1{100000000 + i}",
                    Note = "Nha cung cap phan phoi hang tieu dung",
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
                var name = $"Sieu thi / Dai ly {city} {i:D4}";
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
                    Email = $"daily{i:D4}@retail.vn",
                    Address = $"{200 + i} {Streets[i % Streets.Length]}, {city}",
                    TaxCode = $"2{200000000 + i}",
                    Note = "Khach hang ban le / phan phoi",
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
                var p2 = productsAll[i % productsAll.Count];

                var details = new List<ImportReceiptDetail>
                {
                    new()
                    {
                        ProductId = p1.Id,
                        Quantity = 10 + (i % 40),
                        UnitPrice = p1.Price * 0.7m,
                        LineTotal = Math.Round((10 + (i % 40)) * p1.Price * 0.7m, 2),
                        Note = "Dong nhap chinh"
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
                    Code = code,
                    ReceiptDate = DateTime.UtcNow.AddDays(-Target + i),
                    WarehouseId = wh.Id,
                    SupplierId = sup.Id,
                    Status = status,
                    Note = $"Phieu nhap hang tu {sup.Name}",
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
                var p1 = productsAll[(i - 1) % productsAll.Count];

                receipts.Add(new ExportReceipt
                {
                    Code = code,
                    ReceiptDate = DateTime.UtcNow.AddDays(-Target + i + 2),
                    WarehouseId = wh.Id,
                    CustomerId = cus.Id,
                    Status = status,
                    Note = $"Xuat ban cho {cus.Name}",
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
                            Note = "Xuat theo don"
                        }
                    ]
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
