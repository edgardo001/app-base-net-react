// PermissionsController y ProfileController comparten este archivo
// para mantener el proyecto compacto. ProfileController usa los mismos
// principios: sin EF Core, solo IUnitOfWork para acceso a datos.
// AdminController hereda el mismo patron y centraliza endpoints de
// administracion (dashboard, audit log, revoke tokens global).
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagement.Application.Common.Interfaces;
using UserManagement.Domain.Entities;
using UserManagement.WebApi.Filters;

namespace UserManagement.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public PermissionsController(IUnitOfWork uow)
    {
        _uow = uow;
    }

    [HttpGet]
    public async Task<IActionResult> GetPermissions(CancellationToken ct)
    {
        var permissions = await _uow.Roles.GetAllAsync(ct);
        // Use a dedicated repository for permissions in production
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    [HttpGet("modules")]
    public async Task<IActionResult> GetModules(CancellationToken ct)
    {
        return Ok(ApiResponse<object>.Ok(new { }));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public ProfileController(IUnitOfWork uow)
    {
        _uow = uow;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _uow.Users.GetByIdWithRolesAsync(userId, ct);
        if (user == null) return NotFound();

        return Ok(ApiResponse<object>.Ok(new
        {
            user.Id, user.Email, user.FirstName, user.LastName,
            user.AvatarPath, user.LastLoginAt, user.LastPasswordChangeAt,
            user.CreatedAt,
            Roles = user.UserRoles.Select(ur => new { ur.Role.Id, ur.Role.Name })
        }));
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _uow.Users.GetByIdAsync(userId, ct);
        if (user == null) return NotFound();

        user.UpdateProfile(request.FirstName, request.LastName);
        await _uow.SaveChangesAsync(ct);

        return Ok(ApiResponse<object>.Ok(null, "Profile updated"));
    }

    [HttpGet("activity")]
    public async Task<IActionResult> GetActivity(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var logs = await _uow.AuditLogs.GetByUserAsync(userId, 50, ct);
        return Ok(ApiResponse<object>.Ok(logs.Select(l => new
        {
            l.Action, l.EntityType, l.EntityId, l.Details, l.CreatedAt
        })));
    }
}

public record UpdateProfileRequest(string FirstName, string LastName);

[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public AdminController(IUnitOfWork uow)
    {
        _uow = uow;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var totalUsers = await _uow.Users.CountAsync(null, ct);
        var activeUsers = await _uow.Users.CountAsync(u => u.IsActive, ct);
        var newUsers = await _uow.Users.CountAsync(
            u => u.CreatedAt >= DateTime.UtcNow.AddDays(-7), ct);

        return Ok(ApiResponse<object>.Ok(new
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            NewUsersLast7Days = newUsers,
            InactiveUsers = totalUsers - activeUsers
        }));
    }

    [HttpGet("audit-log")]
    public async Task<IActionResult> GetAuditLog(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _uow.AuditLogs.GetPagedAsync(page, pageSize, null, "CreatedAt", true, null, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPost("revoke-all-tokens")]
    public async Task<IActionResult> RevokeAllTokens(CancellationToken ct)
    {
        await _uow.RefreshTokens.RevokeAllGlobalAsync(null, ct);
        await _uow.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(null, "All sessions revoked globally"));
    }
}
