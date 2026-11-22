using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Infrastructure.Auth;
using WarehouseManagement.Infrastructure.Persistence;
using WarehouseManagement.Infrastructure.Repositories;
using WarehouseManagement.Infrastructure.Storage;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1) Chuỗi kết nối SQL Server (appsettings.json → ConnectionStrings:DefaultConnection)
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Chưa cấu hình DefaultConnection.");

        // 2) EF Core DbContext
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        // 3) JWT + lưu ảnh (DB media)
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IFileStorageService, DatabaseFileStorageService>();

        // Đăng ký repository (scoped theo request)
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IUnitRepository, UnitRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IWarehouseStockRepository, WarehouseStockRepository>();
        services.AddScoped<IImportReceiptRepository, ImportReceiptRepository>();
        services.AddScoped<IExportReceiptRepository, ExportReceiptRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IStocktakeRepository, StocktakeRepository>();
        services.AddScoped<IStocktakeDetailRepository, StocktakeDetailRepository>();
        services.AddScoped<IStockTransactionRepository, StockTransactionRepository>();
        return services;
    }
}
