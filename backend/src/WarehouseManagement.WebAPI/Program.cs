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
        options.Events = new JwtBearerEvents
        {
            // Cho phép dán nhầm "Bearer <token>" vào ô Swagger (tránh "Bearer Bearer ...")
            OnMessageReceived = context =>
            {
                var header = context.Request.Headers.Authorization.ToString();
                if (string.IsNullOrWhiteSpace(header))
                    return Task.CompletedTask;

                const string prefix = "Bearer ";
                if (!header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return Task.CompletedTask;

                var rest = header[prefix.Length..].Trim();
                while (rest.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    rest = rest[prefix.Length..].Trim();

                if (!string.IsNullOrWhiteSpace(rest))
                    context.Token = rest;

                return Task.CompletedTask;
            },
            // Token hết hạn/sai: coi như chưa đăng nhập (để [AllowAnonymous] catalog vẫn chạy)
            OnAuthenticationFailed = context =>
            {
                context.NoResult();
                return Task.CompletedTask;
            },
            // 401 có body JSON (Swagger không còn "Undocumented" trống)
            OnChallenge = async context =>
            {
                context.HandleResponse();
                if (context.Response.HasStarted)
                    return;

                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(
                    ApiResponse<object>.Fail(
                        "Chưa đăng nhập hoặc token không hợp lệ. Swagger: POST /auth/admin/login → copy data.token → nút Authorize → chỉ dán token → Authorize."));
            },
            OnForbidden = async context =>
            {
                if (context.Response.HasStarted)
                    return;

                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(
                    ApiResponse<object>.Fail("Không đủ quyền truy cập API này."));
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
        Version = "v1",
        Description =
            "Cách test: 1) POST /api/v1/auth/admin/login (admin/123456) → " +
            "2) Hệ thống tự Authorize token (hoặc bấm ổ khóa dán token) → " +
            "3) GET/POST/PUT/DELETE. Curl phải có: Authorization: Bearer ..."
    });

    // Nút Authorize: CHỈ dán token (Swagger tự thêm chữ Bearer)
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description =
            "1) POST /api/v1/auth/admin/login (admin / 123456)\n" +
            "2) Copy data.token\n" +
            "3) Dán VÀO ĐÂY chỉ phần token (không gõ thêm chữ Bearer)",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    // Bắt buộc truyền OpenApiDocument vào Reference — nếu không, swagger.json ra security: [{}]
    // → Swagger UI Authorize xong vẫn KHÔNG gửi header Authorization (CRUD bị 401).
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
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
// Bắt exception domain → map HTTP status + ApiResponse.Fail
// Không nuốt lỗi Swagger (để dễ debug schema/controller)
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        // Swagger/OpenAPI: trả message thật (không giấu thành "lỗi hệ thống")
        if (context.Request.Path.StartsWithSegments("/swagger"))
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync(ex.ToString());
            return;
        }

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

app.UseHttpsRedirection();
app.UseStaticFiles(); // /uploads/... + /swagger-ui/auto-authorize.js

// Dev: Swagger UI — tự gắn JWT sau login (xem wwwroot/swagger-ui/auto-authorize.js)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.EnablePersistAuthorization();
        options.DisplayRequestDuration();
        options.InjectJavascript("/swagger-ui/auto-authorize.js");
    });
}

app.UseCors("Frontend");
app.UseAuthentication(); // đọc JWT
app.UseAuthorization();  // kiểm [Authorize]
app.MapControllers();
app.Run();
