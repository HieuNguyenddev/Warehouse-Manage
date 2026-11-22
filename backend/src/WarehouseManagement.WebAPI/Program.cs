using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using WarehouseManagement.Application;
using WarehouseManagement.Infrastructure;
using WarehouseManagement.Infrastructure.Persistence;
using WarehouseManagement.Shared;
using WarehouseManagement.WebAPI.Routing;

var builder = WebApplication.CreateBuilder(args);

// Controllers + chuyển [controller] sang kebab-case (WarehouseStocks -> warehouse-stocks)
builder.Services.AddControllers(options =>
    {
        // [controller] => kebab-case: WarehouseStocks -> warehouse-stocks
        options.Conventions.Add(
            new RouteTokenTransformerConvention(new SlugifyParameterTransformer()));
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Model validation lỗi → trả ApiResponse thống nhất (400)
        options.InvalidModelStateResponseFactory = context =>
        {
            var message = string.Join(" ",
                context.ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage)
                        ? e.Exception?.Message
                        : e.ErrorMessage)
                    .Where(m => !string.IsNullOrWhiteSpace(m)));

            if (string.IsNullOrWhiteSpace(message))
                message = "Dữ liệu không hợp lệ.";

            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(
                ApiResponse<object>.Fail(message!));
        };
    });

// ===== JWT Authentication =====
// Đọc JwtSettings từ appsettings; xác thực Bearer token cho các endpoint [Authorize]
var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("Chưa cấu hình Jwt trong appsettings.json.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.Zero
        };
        // Token hết hạn/sai: coi như chưa đăng nhập (để [AllowAnonymous] catalog vẫn chạy)
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                context.NoResult();
                return Task.CompletedTask;
            }
        };
    });

// Phân quyền theo role (AdminPortal, Customer, ...)
builder.Services.AddAuthorization();

// Swagger/OpenAPI + nút Authorize dán JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Warehouse Management API",
        Version = "v1"
    });

    // Nút Authorize trên Swagger: dán token vào
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập JWT theo dạng: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer")] = []
    });
});

// ===== CORS =====
// Cho phép frontend Vite (dev/preview) gọi API từ origin khác
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "http://localhost:4173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// Đăng ký Application (use-case) + Infrastructure (EF, JWT, file, ...)
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// ===== Seed =====
// Seed role + tài khoản mẫu (chạy một lần khi khởi động)
await DataSeeder.SeedAsync(app.Services);

// ===== Middleware =====
// Bắt exception domain → map HTTP status + ApiResponse.Fail (không lộ chi tiết 500)
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var status = ex switch
        {
            NotFoundException => (int)HttpStatusCode.NotFound,
            ConflictException => (int)HttpStatusCode.Conflict,
            ValidationException => (int)HttpStatusCode.BadRequest,
            UnauthorizedException => (int)HttpStatusCode.Unauthorized,
            _ => (int)HttpStatusCode.InternalServerError
        };

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";

        var message = status == 500 ? "Đã xảy ra lỗi hệ thống." : ex.Message;
        await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(message));
    }
});

// Dev: mở Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // phục vụ /uploads/avatars/...
app.UseCors("Frontend");
app.UseAuthentication(); // đọc JWT
app.UseAuthorization();  // kiểm [Authorize]
app.MapControllers();
app.Run();
