using Microsoft.Extensions.DependencyInjection;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Application.Services;

namespace WarehouseManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // --- Tài khoản / đăng nhập ---
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();

        // --- Master data ---
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IUnitService, UnitService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IWarehouseService, WarehouseService>();

        // --- Tồn kho ---
        services.AddScoped<IWarehouseStockService, WarehouseStockService>();
        services.AddScoped<IStockLotService, StockLotService>();
        services.AddScoped<IStockMovementService, StockMovementService>();
        services.AddScoped<IStockTransactionService, StockTransactionService>();

        // --- Phiếu / cửa hàng / kiểm kê ---
        services.AddScoped<IImportReceiptService, ImportReceiptService>();
        services.AddScoped<IExportReceiptService, ExportReceiptService>();
        services.AddScoped<IStoreCheckoutService, StoreCheckoutService>();
        services.AddScoped<IStocktakeService, StocktakeService>();
        services.AddScoped<IStocktakeDetailService, StocktakeDetailService>();

        return services;
    }
}
