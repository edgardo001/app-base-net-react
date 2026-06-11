using System.Security.Cryptography;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Auth.Commands.GoogleLogin;
using AppBaseNetReact.WebApi.Filters;
using Microsoft.Extensions.Logging;

namespace AppBaseNetReact.WebApi.Controllers;

[ApiController]
[Route("api/auth/google")]
[EnableRateLimiting("Google")]
public class GoogleAuthController : ControllerBase
{
    private readonly IGoogleAuthService _googleAuth;
    private readonly IMediator _mediator;
    private readonly string _frontendUrl;
    private readonly ILogger<GoogleAuthController> _logger;

    public GoogleAuthController(IGoogleAuthService googleAuth, IMediator mediator, IConfiguration configuration, ILogger<GoogleAuthController> logger)
    {
        _googleAuth = googleAuth;
        _mediator = mediator;
        _frontendUrl = configuration["FrontendUrl"] ?? "http://localhost:5173";
        _logger = logger;
    }

    [HttpGet("login")]
    public IActionResult Login()
    {
        var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var authUrl = _googleAuth.GetAuthorizationUrl(state);
        return Redirect(authUrl);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string code,
        [FromQuery] string state,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            return Redirect($"{_frontendUrl}/login?error=google_auth_failed");
        }

        var command = new GoogleLoginCommand(
            code,
            state,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            _frontendUrl);

        var outcome = await _mediator.Send(command, ct);

        _logger.LogInformation("Google callback outcome: IsSuccess={IsSuccess}, ErrorCode={ErrorCode}, HasAccessToken={HasAccessToken}",
            outcome.Result.IsSuccess, outcome.Result.ErrorCode, outcome.AccessToken != null);

        if (!outcome.Result.IsSuccess)
        {
            _logger.LogWarning("Google login failed: {Message}", outcome.Result.ErrorMessage);
            return Redirect($"{_frontendUrl}/login?error=google_auth_failed");
        }

        _logger.LogInformation("Google login success, redirecting to /oauth-callback");
        return Redirect(
            $"{_frontendUrl}/oauth-callback#accessToken={Uri.EscapeDataString(outcome.AccessToken)}&refreshToken={Uri.EscapeDataString(outcome.RefreshToken)}&expiresAt={new DateTimeOffset(outcome.ExpiresAt!.Value).ToUnixTimeSeconds()}");
    }
}
