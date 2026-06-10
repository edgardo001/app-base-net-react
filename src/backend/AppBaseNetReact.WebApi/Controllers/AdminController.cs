using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using AppBaseNetReact.Application.Features.Admin.Commands.RevokeAllTokens;
using AppBaseNetReact.Application.Features.Admin.Commands.SendTestEmail;
using AppBaseNetReact.Application.Features.Admin.Queries.GetAuditLog;
using AppBaseNetReact.Application.Features.Admin.Queries.GetDashboard;
using AppBaseNetReact.Infrastructure.Email;
using AppBaseNetReact.Infrastructure.Services;
using AppBaseNetReact.WebApi.Filters;

namespace AppBaseNetReact.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly EmailRenderer _renderer;
    private readonly EmailOptions _emailOptions;

    public AdminController(
        IMediator mediator,
        EmailRenderer renderer,
        IOptions<EmailOptions> emailOptions)
    {
        _mediator = mediator;
        _renderer = renderer;
        _emailOptions = emailOptions.Value;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDashboardQuery(), ct);
        return Ok(ApiResponse<GetDashboardResponse>.Ok(result));
    }

    [HttpGet("audit-log")]
    public async Task<IActionResult> GetAuditLog(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAuditLogQuery(page, pageSize), ct);
        return Ok(ApiResponse<GetAuditLogResponse>.Ok(result));
    }

    [HttpPost("revoke-all-tokens")]
    public async Task<IActionResult> RevokeAllTokens(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        Guid? userId = null;
        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var uid))
            userId = uid;

        var outcome = await _mediator.Send(new RevokeAllTokensCommand(
            userId,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers.UserAgent.ToString()), ct);

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

        var htmlBody = _renderer.Render(config.TemplateFile, vars);

        var userIdClaim = User.FindFirst("sub")?.Value;
        Guid? userId = null;
        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var uid))
            userId = uid;

        var outcome = await _mediator.Send(new SendTestEmailCommand(
            request.To, config.Subject, htmlBody, userId,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers.UserAgent.ToString()), ct);

        if (!outcome.Result.IsSuccess)
        {
            return StatusCode(500, ApiResponse<object>.Fail(
                $"Failed to send test email: {outcome.Result.ErrorMessage}"));
        }

        return Ok(ApiResponse<object>.Ok($"Test email sent to {request.To}"));
    }
}

public record SendTestEmailRequest(string To);
