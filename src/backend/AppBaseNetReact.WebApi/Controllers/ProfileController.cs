using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Common.Validators;
using AppBaseNetReact.WebApi.Filters;

namespace AppBaseNetReact.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;

    public ProfileController(IUnitOfWork uow, IAuditService audit)
    {
        _uow = uow;
        _audit = audit;
    }

[HttpGet]
public async Task<IActionResult> GetProfile(CancellationToken ct)
{
    var userIdClaim = User.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        return Unauthorized();

    var user = await _uow.Users.GetByIdAsync(userId, ct);
    if (user == null)
        return NotFound();

    return Ok(ApiResponse<object>.Ok(new
    {
        user.Id,
        user.Email,
        user.FirstName,
        user.LastName,
        user.AvatarPath
    }));
}

[HttpGet("activity")]
public async Task<IActionResult> GetActivity(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var logs = await _uow.AuditLogs.GetByUserAsync(userId, 20, ct);
        var items = logs.Select(l => new
        {
            l.Action,
            l.EntityType,
            l.Details,
            l.CreatedAt
        });

        return Ok(ApiResponse<object>.Ok(items));
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _uow.Users.GetByIdAsync(userId, ct);
        if (user == null)
            return NotFound();

        var oldFirstName = user.FirstName;
        var oldLastName = user.LastName;
        var oldValues = $"{{\"firstName\":\"{oldFirstName}\",\"lastName\":\"{oldLastName}\"}}";
        var newValues = $"{{\"firstName\":\"{request.FirstName}\",\"lastName\":\"{request.LastName}\"}}";

        user.UpdateProfile(request.FirstName, request.LastName);
        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "ProfileUpdated", "User", user.Id.ToString(),
            oldValues, newValues, userId,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers.UserAgent.ToString(), null, ct);

        return Ok(ApiResponse<object>.Ok(null, "Profile updated"));
    }
}
