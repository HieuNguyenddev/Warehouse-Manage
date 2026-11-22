// API kiểm tra sống/chết của dịch vụ (health check).
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Shared;

namespace WarehouseManagement.WebAPI.Controllers;

// Endpoint công khai để monitoring / load balancer kiểm tra API còn chạy
[ApiController]
[Route("api/v1/health")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    // GET /api/v1/health
    // Mục đích: trả trạng thái ok + thời gian UTC
    // Auth: AllowAnonymous
    // Side effect: không (chỉ đọc)
    [HttpGet]
    public IActionResult Get()
        => Ok(ApiResponse<object>.Ok(new { status = "ok", time = DateTime.UtcNow }, "API đang chạy"));
}
