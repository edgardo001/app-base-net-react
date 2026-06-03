using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Domain.Entities;
using AppBaseNetReact.WebApi.Filters;

namespace AppBaseNetReact.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class AdminController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;

    public AdminController(IUnitOfWork uow, IAuditService audit)
    {
        _uow = uow;
        _audit = audit;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var totalUsers = await _uow.Users.CountAsync(null, ct);
        var activeUsers = await _uow.Users.CountAsync(u => u.IsActive, ct);
        var inactiveUsers = await _uow.Users.CountAsync(u => !u.IsActive, ct);
        var newUsersLast7Days = await _uow.Users.CountAsync(
            u => u.CreatedAt >= DateTime.UtcNow.AddDays(-7), ct);

        return Ok(ApiResponse<object>.Ok(new
        {
            totalUsers,
            activeUsers,
            inactiveUsers,
            newUsersLast7Days
        }));
    }

    [HttpGet("audit-log")]
    public async Task<IActionResult> GetAuditLog(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _uow.AuditLogs.GetPagedAsync(page, pageSize, ct: ct);
        return Ok(ApiResponse<object>.Ok(new
        {
            items = result.Items.Select(l => new
            {
                l.Action,
                l.EntityType,
                l.EntityId,
                l.Details,
                l.UserId,
                l.CreatedAt
            }),
            result.TotalCount,
            result.Page,
            result.PageSize,
            result.TotalPages
        }));
    }

    [HttpPost("revoke-all-tokens")]
    public async Task<IActionResult> RevokeAllTokens(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        Guid? userId = null;
        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var uid))
            userId = uid;

        await _uow.RefreshTokens.RevokeAllGlobalAsync(userId, ct);
        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "AllTokensRevoked", "System", null,
            null, null, userId,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers.UserAgent.ToString(), null, ct);

        return Ok(ApiResponse<object>.Ok(null, "All sessions revoked"));
    }
}
