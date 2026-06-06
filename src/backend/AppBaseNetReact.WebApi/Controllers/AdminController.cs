using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Domain.Entities;
using AppBaseNetReact.Infrastructure.Email;
using AppBaseNetReact.Infrastructure.Services;
using AppBaseNetReact.WebApi.Filters;

namespace AppBaseNetReact.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class AdminController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;
    private readonly IEmailService _email;
    private readonly EmailRenderer _renderer;
    private readonly EmailOptions _emailOptions;

    public AdminController(
        IUnitOfWork uow,
        IAuditService audit,
        IEmailService email,
        EmailRenderer renderer,
        IOptions<EmailOptions> emailOptions)
    {
        _uow = uow;
        _audit = audit;
        _email = email;
        _renderer = renderer;
        _emailOptions = emailOptions.Value;
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

        return Ok(ApiResponse<object>.Ok("All sessions revoked"));
    }

    [HttpPost("test-email")]
    public async Task<IActionResult> SendTestEmail([FromBody] SendTestEmailRequest request, CancellationToken ct)
    {
        if (!_emailOptions.Templates.TryGetValue("TestEmail", out var config))
            return BadRequest(ApiResponse<object>.Fail("Test email template not configured"));

        var adminName = User.FindFirst("name")?.Value ?? "Administrador";
        var smtpHost = string.IsNullOrEmpty(_emailOptions.Smtp.Host) ? "No configurado" : _emailOptions.Smtp.Host;
        var smtpPort = _emailOptions.Smtp.Port;

        var vars = new Dictionary<string, string>
        {
            ["AdminName"] = adminName,
            ["DateTime"] = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss UTC"),
            ["SmtpHost"] = smtpHost,
            ["SmtpPort"] = smtpPort.ToString(),
            ["Year"] = DateTime.UtcNow.Year.ToString()
        };

        try
        {
            var htmlBody = _renderer.Render(config.TemplateFile, vars);
            await _email.SendEmailAsync(request.To, config.Subject, htmlBody, ct);

            var userIdClaim = User.FindFirst("sub")?.Value;
            Guid? userId = null;
            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var uid))
                userId = uid;

            await _audit.LogAsync(
                "TestEmailSent", "Email", null,
                $"Test email sent to {request.To}", null, userId,
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                Request.Headers.UserAgent.ToString(), null, ct);

            return Ok(ApiResponse<object>.Ok($"Test email sent to {request.To}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail($"Failed to send test email: {ex.Message}"));
        }
    }
}

public record SendTestEmailRequest(string To);
