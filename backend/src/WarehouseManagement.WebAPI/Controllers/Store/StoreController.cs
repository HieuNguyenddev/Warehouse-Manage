// API cổng cửa hàng khách hàng: catalog công khai + hồ sơ/checkout cần JWT Customer.
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Application.Interfaces;
using WarehouseManagement.Shared;
using WarehouseManagement.WebAPI.Helpers;

namespace WarehouseManagement.WebAPI.Controllers;

// Catalog AllowAnonymous; me/checkout/invoices yêu cầu role Customer
[ApiController]
[Route("api/v1/[controller]")]
public class StoreController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ICustomerService _customerService;
    private readonly ICategoryService _categoryService;
    private readonly IStoreCheckoutService _checkoutService;

    public StoreController(
        IProductService productService,
        ICustomerService customerService,
        ICategoryService categoryService,
        IStoreCheckoutService checkoutService)
    {
        _productService = productService;
        _customerService = customerService;
        _categoryService = categoryService;
        _checkoutService = checkoutService;
    }

    // GET /api/v1/store/products
    // Mục đích: catalog sản phẩm đang active (công khai)
    // Auth: AllowAnonymous
    // Side effect: không (chỉ đọc; lọc IsActive)
    [AllowAnonymous]
    [HttpGet("products")]
    public async Task<IActionResult> GetProducts(
        [FromQuery] PaginationQuery query,
        [FromQuery] int? categoryId = null)
    {
        var result = await _productService.GetAllAsync(query, categoryId);
        result.Items = result.Items.Where(x => x.IsActive).ToList();
        return Ok(ApiResponse<PagedResult<ProductResponseDto>>.Ok(result));
    }

    // GET /api/v1/store/products/{id}
    // Mục đích: chi tiết sản phẩm active
    // Auth: AllowAnonymous
    // Side effect: không (chỉ đọc; 404 nếu inactive)
    [AllowAnonymous]
    [HttpGet("products/{id:int}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        var result = await _productService.GetByIdAsync(id);
        if (!result.IsActive)
            return NotFound(ApiResponse<object>.Fail("Sản phẩm không khả dụng."));
        return Ok(ApiResponse<ProductResponseDto>.Ok(result));
    }

    // GET /api/v1/store/categories
    // Mục đích: danh mục active cho storefront
    // Auth: AllowAnonymous
    // Side effect: không (chỉ đọc; pageSize tối thiểu 100)
    [AllowAnonymous]
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories([FromQuery] PaginationQuery query)
    {
        query.PageSize = Math.Max(query.PageSize, 100);
        var result = await _categoryService.GetAllAsync(query);
        result.Items = result.Items.Where(x => x.IsActive).ToList();
        return Ok(ApiResponse<PagedResult<CategoryResponseDto>>.Ok(result));
    }

    // GET /api/v1/store/me
    // Mục đích: lấy hồ sơ khách hàng gắn với user đăng nhập
    // Auth: JWT + role Customer
    // Side effect: không (chỉ đọc)
    [Authorize(Roles = AppRoles.Customer)]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = RequireUserId();
        var me = await _customerService.GetByUserIdAsync(userId);
        return Ok(ApiResponse<CustomerResponseDto>.Ok(me));
    }

    // PUT /api/v1/store/me
    // Mục đích: cập nhật hồ sơ khách hàng tự phục vụ
    // Auth: JWT + role Customer
    // Side effect: cập nhật DB theo userId từ token
    [Authorize(Roles = AppRoles.Customer)]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] CustomerProfileUpdateDto dto)
    {
        var userId = RequireUserId();
        var me = await _customerService.UpdateProfileByUserIdAsync(userId, dto);
        return Ok(ApiResponse<CustomerResponseDto>.Ok(me, "Cập nhật hồ sơ thành công"));
    }

    // POST /api/v1/store/me/avatar — nhận IFormFile, lưu DB media
    [Authorize(Roles = AppRoles.Customer)]
    [HttpPost("me/avatar")]
    [RequestSizeLimit(2 * 1024 * 1024)]
    // Không gắn [FromForm] trên IFormFile — Swagger/Swashbuckle sẽ lỗi generate
    public async Task<IActionResult> UploadMyAvatar(IFormFile? file)
    {
        FormFileHelper.EnsureValidImage(file);
        var userId = RequireUserId();
        await using var stream = file!.OpenReadStream();
        var me = await _customerService.UploadAvatarByUserIdAsync(userId, stream, file.FileName, file.ContentType);
        return Ok(ApiResponse<CustomerResponseDto>.Ok(me, "Cập nhật ảnh đại diện thành công"));
    }

    // POST /api/v1/store/checkout
    // Mục đích: đặt hàng từ storefront
    // Auth: JWT + role Customer
    // Side effect: tạo phiếu xuất Source=Store + trừ tồn + tạo hóa đơn
    [Authorize(Roles = AppRoles.Customer)]
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] StoreCheckoutRequestDto dto)
    {
        var result = await _checkoutService.CheckoutAsync(RequireUserId(), dto);
        return Ok(ApiResponse<StoreCheckoutResponseDto>.Ok(result, "Đặt hàng thành công"));
    }

    // GET /api/v1/store/invoices
    // Mục đích: danh sách hóa đơn của khách đang đăng nhập
    // Auth: JWT + role Customer
    // Side effect: không (chỉ đọc)
    [Authorize(Roles = AppRoles.Customer)]
    [HttpGet("invoices")]
    public async Task<IActionResult> MyInvoices([FromQuery] PaginationQuery query)
    {
        var result = await _checkoutService.GetMyInvoicesAsync(RequireUserId(), query);
        return Ok(ApiResponse<PagedResult<InvoiceResponseDto>>.Ok(result));
    }

    // GET /api/v1/store/invoices/{id}
    // Mục đích: chi tiết một hóa đơn của chính mình
    // Auth: JWT + role Customer
    // Side effect: không (chỉ đọc)
    [Authorize(Roles = AppRoles.Customer)]
    [HttpGet("invoices/{id:int}")]
    public async Task<IActionResult> MyInvoice(int id)
    {
        var result = await _checkoutService.GetMyInvoiceAsync(RequireUserId(), id);
        return Ok(ApiResponse<InvoiceResponseDto>.Ok(result));
    }

    // Lấy userId từ claim JWT; ném UnauthorizedException nếu token không hợp lệ
    private int RequireUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId) || userId <= 0)
            throw new UnauthorizedException("Token không hợp lệ.");
        return userId;
    }
}
