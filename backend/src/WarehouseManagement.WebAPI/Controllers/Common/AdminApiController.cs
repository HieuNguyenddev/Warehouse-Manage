// Base controller cho toàn bộ API quản trị kho.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Shared;

namespace WarehouseManagement.WebAPI.Controllers;

// Yêu cầu JWT + role AdminPortal; route mặc định api/v1/[controller]
[Authorize(Roles = AppRoles.AdminPortal)]
[ApiController]
[Route("api/v1/[controller]")]
public abstract class AdminApiController : ControllerBase
{
}
