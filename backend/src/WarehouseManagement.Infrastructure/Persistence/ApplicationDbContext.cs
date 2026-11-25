using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Domain;

namespace WarehouseManagement.Infrastructure.Persistence;

/// <summary>
/// DbContext chính — ánh xạ toàn bộ entity nghiệp vụ sang SQL Server.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // ----- DbSet: bảng dữ liệu -----
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<WarehouseStock> WarehouseStocks => Set<WarehouseStock>();
    public DbSet<StockLot> StockLots => Set<StockLot>();
    public DbSet<ImportReceipt> ImportReceipts => Set<ImportReceipt>();
    public DbSet<ImportReceiptDetail> ImportReceiptDetails => Set<ImportReceiptDetail>();
    public DbSet<ExportReceipt> ExportReceipts => Set<ExportReceipt>();
    public DbSet<ExportReceiptDetail> ExportReceiptDetails => Set<ExportReceiptDetail>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceDetail> InvoiceDetails => Set<InvoiceDetail>();
    public DbSet<Stocktake> Stocktakes => Set<Stocktake>();
    public DbSet<StocktakeDetail> StocktakeDetails => Set<StocktakeDetail>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>(); // ảnh upload lưu DB

    /// <summary>
    /// Cấu hình bảng, khóa, chỉ mục và quan hệ giữa các entity.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ===== Vai trò =====
        modelBuilder.Entity<Role>(e =>
        {
            e.ToTable("Roles");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(50); // Tên vai trò (Admin, Staff...)
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Description).HasMaxLength(255);
        });

        // ===== Người dùng =====
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.Id);

            e.Property(x => x.Username).IsRequired().HasMaxLength(50);
            e.HasIndex(x => x.Username).IsUnique(); // Username không trùng

            e.Property(x => x.PasswordHash).IsRequired().HasMaxLength(200); // Mật khẩu đã băm

            e.Property(x => x.FullName).IsRequired().HasMaxLength(100);

            e.Property(x => x.Email).IsRequired().HasMaxLength(100);
            e.HasIndex(x => x.Email).IsUnique();

            e.Property(x => x.Phone).HasMaxLength(20);

            e.Property(x => x.IsActive).IsRequired(); // Tài khoản còn hoạt động?

            // User thuộc 1 Role; không xóa Role nếu còn User
            e.HasOne(x => x.Role)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== Danh mục sản phẩm =====
        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Description).HasMaxLength(255);
            e.Property(x => x.IsActive).IsRequired();
        });

        // ===== Đơn vị tính =====
        modelBuilder.Entity<Unit>(e =>
        {
            e.ToTable("Units");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(50);
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Symbol).IsRequired().HasMaxLength(20); // Ký hiệu (kg, l, cai...)
            e.HasIndex(x => x.Symbol).IsUnique();
            e.Property(x => x.Description).HasMaxLength(255);
            e.Property(x => x.IsActive).IsRequired();
        });

        // ===== Nhà cung cấp =====
        modelBuilder.Entity<Supplier>(e =>
        {
            e.ToTable("Suppliers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.ContactName).HasMaxLength(100); // Người liên hệ
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.Email).HasMaxLength(100);
            // Email unique chỉ khi có giá trị (cho phép nhiều NULL)
            e.HasIndex(x => x.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
            e.Property(x => x.Address).HasMaxLength(255);
            e.Property(x => x.TaxCode).HasMaxLength(20); // Mã số thuế
            e.HasIndex(x => x.TaxCode).IsUnique().HasFilter("[TaxCode] IS NOT NULL");
            e.Property(x => x.Note).HasMaxLength(255);
            e.Property(x => x.IsActive).IsRequired();
        });

        // ===== Khách hàng =====
        modelBuilder.Entity<Customer>(e =>
        {
            e.ToTable("Customers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.ContactName).HasMaxLength(100);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.Email).HasMaxLength(100);
            e.HasIndex(x => x.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
            e.Property(x => x.Address).HasMaxLength(255);
            e.Property(x => x.TaxCode).HasMaxLength(20);
            e.HasIndex(x => x.TaxCode).IsUnique().HasFilter("[TaxCode] IS NOT NULL");
            e.Property(x => x.Note).HasMaxLength(255);
            e.Property(x => x.AvatarUrl).HasMaxLength(255); // Đường dẫn ảnh đại diện
            e.Property(x => x.IsActive).IsRequired();

            // Liên kết 1-1 với User (tài khoản portal khách); unique khi có UserId
            e.HasIndex(x => x.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL");
            e.HasOne(x => x.User)
                .WithOne(x => x.Customer)
                .HasForeignKey<Customer>(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull); // Xóa User → gỡ liên kết, giữ Customer
        });

        // ===== Sản phẩm =====
        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("Products");
            e.HasKey(x => x.Id);

            e.Property(x => x.Sku).IsRequired().HasMaxLength(50); // Mã SKU
            e.HasIndex(x => x.Sku).IsUnique();

            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.Price).HasColumnType("decimal(18,2)"); // Giá bán
            e.Property(x => x.MinStock).IsRequired(); // Ngưỡng tồn tối thiểu mặc định
            e.Property(x => x.IsActive).IsRequired();

            // Thuộc tính dược BV
            e.Property(x => x.RegistrationNumber).HasMaxLength(50);
            e.Property(x => x.ActiveIngredient).HasMaxLength(200);
            e.Property(x => x.Strength).HasMaxLength(100);
            e.Property(x => x.DosageForm).HasMaxLength(100);
            e.Property(x => x.Manufacturer).HasMaxLength(200);
            e.Property(x => x.IsControlledSubstance).IsRequired();
            e.Property(x => x.RequiresColdStorage).IsRequired();
            e.Property(x => x.ExpiryAlertDays).IsRequired();

            e.HasIndex(x => x.RegistrationNumber);
            e.HasIndex(x => x.ActiveIngredient);
            e.HasIndex(x => x.IsControlledSubstance);

            e.HasOne(x => x.Category)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Unit)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== Ảnh sản phẩm =====
        modelBuilder.Entity<ProductImage>(e =>
        {
            e.ToTable("ProductImages");
            e.HasKey(x => x.Id);

            e.Property(x => x.ImageUrl).IsRequired().HasMaxLength(255);
            e.Property(x => x.SortOrder).IsRequired(); // Thứ tự hiển thị
            e.Property(x => x.IsPrimary).IsRequired(); // Ảnh chính?

            // Xóa sản phẩm → xóa luôn ảnh
            e.HasOne(x => x.Product)
                .WithMany(x => x.Images)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== Kho =====
        modelBuilder.Entity<Warehouse>(e =>
        {
            e.ToTable("Warehouses");
            e.HasKey(x => x.Id);

            e.Property(x => x.Code).IsRequired().HasMaxLength(30);
            e.HasIndex(x => x.Code).IsUnique();

            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.HasIndex(x => x.Name).IsUnique();

            e.Property(x => x.Address).HasMaxLength(255);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.ManagerName).HasMaxLength(100); // Tên quản lý kho
            e.Property(x => x.Note).HasMaxLength(255);
            e.Property(x => x.IsActive).IsRequired();
            e.Property(x => x.WarehouseType).IsRequired();
            e.HasIndex(x => x.WarehouseType);
        });

        // ===== Tồn kho theo kho + sản phẩm =====
        modelBuilder.Entity<WarehouseStock>(e =>
        {
            e.ToTable("WarehouseStocks");
            e.HasKey(x => x.Id);

            e.Property(x => x.Quantity).HasColumnType("decimal(18,2)"); // Số lượng tồn
            e.Property(x => x.MinQuantity); // Ngưỡng tối thiểu riêng (nullable → dùng Product.MinStock)

            // Mỗi sản phẩm chỉ có 1 dòng tồn trong 1 kho
            e.HasIndex(x => new { x.WarehouseId, x.ProductId }).IsUnique();

            e.HasOne(x => x.Warehouse)
                .WithMany(x => x.Stocks)
                .HasForeignKey(x => x.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Product)
                .WithMany(x => x.Stocks)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== Phiếu nhập =====
        modelBuilder.Entity<ImportReceipt>(e =>
        {
            e.ToTable("ImportReceipts");
            e.HasKey(x => x.Id);

            e.Property(x => x.Code).IsRequired().HasMaxLength(30);
            e.HasIndex(x => x.Code).IsUnique();

            e.Property(x => x.ReceiptDate).IsRequired(); // Ngày nhập
            e.Property(x => x.Status).IsRequired(); // Draft / Confirmed / Cancelled
            e.Property(x => x.Note).HasMaxLength(255);
            e.Property(x => x.ShippingFee).HasColumnType("decimal(18,2)");
            e.Property(x => x.TradeDiscount).HasColumnType("decimal(18,2)");
            e.Property(x => x.NonRefundableTax).HasColumnType("decimal(18,2)");
            e.Property(x => x.PurchaseInvoiceCode).HasMaxLength(30);
            e.HasIndex(x => x.PurchaseInvoiceCode)
                .IsUnique()
                .HasFilter("[PurchaseInvoiceCode] IS NOT NULL");

            e.HasOne(x => x.Warehouse)
                .WithMany(x => x.ImportReceipts)
                .HasForeignKey(x => x.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Supplier)
                .WithMany(x => x.ImportReceipts)
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== Tồn theo lô / hạn dùng (FEFO) =====
        modelBuilder.Entity<StockLot>(e =>
        {
            e.ToTable("StockLots");
            e.HasKey(x => x.Id);

            e.Property(x => x.LotNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
            e.Property(x => x.ExpiryDate);
            e.Property(x => x.ManufactureDate);

            // Cùng kho + thuốc + số lô → 1 dòng (cộng dồn khi nhập thêm cùng lô)
            e.HasIndex(x => new { x.WarehouseId, x.ProductId, x.LotNumber }).IsUnique();
            e.HasIndex(x => new { x.ExpiryDate, x.Quantity }); // Tra cứu cận hạn

            e.HasOne(x => x.Warehouse)
                .WithMany(x => x.Lots)
                .HasForeignKey(x => x.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Product)
                .WithMany(x => x.Lots)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== Chi tiết phiếu nhập =====
        modelBuilder.Entity<ImportReceiptDetail>(e =>
        {
            e.ToTable("ImportReceiptDetails");
            e.HasKey(x => x.Id);

            e.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
            e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)"); // Giá nhập
            e.Property(x => x.SalePrice).HasColumnType("decimal(18,2)"); // Giá bán → Product.Price
            e.Property(x => x.LineTotal).HasColumnType("decimal(18,2)"); // Thành tiền dòng
            e.Property(x => x.LotNumber).HasMaxLength(50);
            e.Property(x => x.ExpiryDate);
            e.Property(x => x.ManufactureDate);
            e.Property(x => x.Note).HasMaxLength(255);

            // Cho phép cùng thuốc nhiều dòng nếu khác số lô (LotNumber null/'' gom một nhóm)
            e.HasIndex(x => new { x.ImportReceiptId, x.ProductId, x.LotNumber });

            e.HasOne(x => x.ImportReceipt)
                .WithMany(x => x.Details)
                .HasForeignKey(x => x.ImportReceiptId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Product)
                .WithMany(x => x.ImportDetails)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== Phiếu xuất =====
        modelBuilder.Entity<ExportReceipt>(e =>
        {
            e.ToTable("ExportReceipts");
            e.HasKey(x => x.Id);

            e.Property(x => x.Code).IsRequired().HasMaxLength(30);
            e.HasIndex(x => x.Code).IsUnique();

            e.Property(x => x.ReceiptDate).IsRequired();
            e.Property(x => x.Status).IsRequired();
            e.Property(x => x.Source).IsRequired(); // Nguồn tạo phiếu (Admin / Store...)
            e.Property(x => x.IssuePurpose).IsRequired();
            e.HasIndex(x => x.Source);
            e.HasIndex(x => x.IssuePurpose);
            e.HasIndex(x => x.CreatedAt);
            e.Property(x => x.Note).HasMaxLength(255);

            e.HasOne(x => x.Warehouse)
                .WithMany(x => x.ExportReceipts)
                .HasForeignKey(x => x.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Customer)
                .WithMany(x => x.ExportReceipts)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== Chi tiết phiếu xuất =====
        modelBuilder.Entity<ExportReceiptDetail>(e =>
        {
            e.ToTable("ExportReceiptDetails");
            e.HasKey(x => x.Id);

            e.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
            e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(x => x.LineTotal).HasColumnType("decimal(18,2)");
            e.Property(x => x.LotNumber).HasMaxLength(50);
            e.Property(x => x.LotAllocationsJson).HasMaxLength(2000);
            e.Property(x => x.Note).HasMaxLength(255);

            e.HasIndex(x => new { x.ExportReceiptId, x.ProductId }).IsUnique();

            e.HasOne(x => x.ExportReceipt)
                .WithMany(x => x.Details)
                .HasForeignKey(x => x.ExportReceiptId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Product)
                .WithMany(x => x.ExportDetails)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== Hóa đơn =====
        modelBuilder.Entity<Invoice>(e =>
        {
            e.ToTable("Invoices");
            e.HasKey(x => x.Id);

            e.Property(x => x.Code).IsRequired().HasMaxLength(30);
            e.HasIndex(x => x.Code).IsUnique();

            e.Property(x => x.InvoiceDate).IsRequired();
            // Snapshot thông tin khách tại thời điểm lập HĐ
            e.Property(x => x.CustomerName).IsRequired().HasMaxLength(150);
            e.Property(x => x.CustomerPhone).HasMaxLength(20);
            e.Property(x => x.CustomerEmail).HasMaxLength(100);
            e.Property(x => x.CustomerAddress).HasMaxLength(255);
            e.Property(x => x.CustomerTaxCode).HasMaxLength(20);
            e.Property(x => x.SubTotal).HasColumnType("decimal(18,2)"); // Tổng tiền hàng
            e.Property(x => x.ShippingFee).HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.TotalQuantity).HasColumnType("decimal(18,2)");
            e.Property(x => x.Status).IsRequired();
            e.Property(x => x.PaymentMethod).IsRequired().HasMaxLength(30);
            e.Property(x => x.Note).HasMaxLength(255);

            // 1 phiếu xuất ↔ tối đa 1 hóa đơn
            e.HasIndex(x => x.ExportReceiptId).IsUnique();

            e.HasOne(x => x.Customer)
                .WithMany(x => x.Invoices)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.ExportReceipt)
                .WithOne(x => x.Invoice)
                .HasForeignKey<Invoice>(x => x.ExportReceiptId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Warehouse)
                .WithMany(x => x.Invoices)
                .HasForeignKey(x => x.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== Chi tiết hóa đơn (snapshot SKU/tên/đơn vị) =====
        modelBuilder.Entity<InvoiceDetail>(e =>
        {
            e.ToTable("InvoiceDetails");
            e.HasKey(x => x.Id);

            e.Property(x => x.ProductSku).IsRequired().HasMaxLength(50);
            e.Property(x => x.ProductName).IsRequired().HasMaxLength(150);
            e.Property(x => x.UnitSymbol).HasMaxLength(20);
            e.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
            e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            e.Property(x => x.LineTotal).HasColumnType("decimal(18,2)");

            e.HasIndex(x => new { x.InvoiceId, x.ProductId }).IsUnique();

            e.HasOne(x => x.Invoice)
                .WithMany(x => x.Details)
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== Phiếu kiểm kê =====
        modelBuilder.Entity<Stocktake>(e =>
        {
            e.ToTable("Stocktakes");
            e.HasKey(x => x.Id);

            e.Property(x => x.Code).IsRequired().HasMaxLength(30);
            e.HasIndex(x => x.Code).IsUnique();

            e.Property(x => x.StocktakeDate).IsRequired();
            e.Property(x => x.Status).IsRequired();
            e.Property(x => x.Note).HasMaxLength(255);

            e.HasOne(x => x.Warehouse)
                .WithMany(x => x.Stocktakes)
                .HasForeignKey(x => x.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== Chi tiết kiểm kê =====
        modelBuilder.Entity<StocktakeDetail>(e =>
        {
            e.ToTable("StocktakeDetails");
            e.HasKey(x => x.Id);

            e.Property(x => x.SystemQuantity).HasColumnType("decimal(18,2)"); // Tồn theo hệ thống
            e.Property(x => x.CountedQuantity).HasColumnType("decimal(18,2)"); // Số đếm thực tế
            e.Property(x => x.Difference).HasColumnType("decimal(18,2)"); // Chênh lệch
            e.Property(x => x.QuantityBeforeConfirm).HasColumnType("decimal(18,2)"); // Tồn trước khi xác nhận
            e.Property(x => x.Note).HasMaxLength(255);

            e.HasIndex(x => new { x.StocktakeId, x.ProductId }).IsUnique();

            e.HasOne(x => x.Stocktake)
                .WithMany(x => x.Details)
                .HasForeignKey(x => x.StocktakeId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Product)
                .WithMany(x => x.StocktakeDetails)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== Lịch sử biến động tồn =====
        modelBuilder.Entity<StockTransaction>(e =>
        {
            e.ToTable("StockTransactions");
            e.HasKey(x => x.Id);

            e.Property(x => x.Type).IsRequired(); // Import / Export / Stocktake / Manual...
            e.Property(x => x.QuantityChange).HasColumnType("decimal(18,2)"); // +/- số lượng
            e.Property(x => x.QuantityBefore).HasColumnType("decimal(18,2)");
            e.Property(x => x.QuantityAfter).HasColumnType("decimal(18,2)");
            e.Property(x => x.ReferenceType).HasMaxLength(50); // Loại chứng từ gốc
            e.Property(x => x.ReferenceCode).HasMaxLength(30); // Mã chứng từ gốc
            e.Property(x => x.Note).HasMaxLength(255);

            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => new { x.WarehouseId, x.ProductId, x.CreatedAt }); // Tra cứu theo kho + SP + thời gian
            e.HasIndex(x => new { x.ReferenceType, x.ReferenceId }); // Tra cứu theo chứng từ

            e.HasOne(x => x.Warehouse)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Product)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Ảnh upload lưu trong DB — PublicId dùng trên URL /api/v1/media/{id}
        modelBuilder.Entity<MediaAsset>(e =>
        {
            e.ToTable("MediaAssets");
            e.HasKey(x => x.Id);
            e.Property(x => x.PublicId).IsRequired();
            e.HasIndex(x => x.PublicId).IsUnique();
            e.Property(x => x.Folder).IsRequired().HasMaxLength(50);
            e.Property(x => x.OriginalFileName).IsRequired().HasMaxLength(255);
            e.Property(x => x.ContentType).IsRequired().HasMaxLength(100);
            e.Property(x => x.Content).IsRequired();
        });
    }
}
