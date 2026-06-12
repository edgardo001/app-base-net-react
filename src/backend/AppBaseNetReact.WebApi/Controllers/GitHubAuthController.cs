using System.Security.Cryptography;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Auth.Commands.GitHubLogin;

namespace AppBaseNetReact.WebApi.Controllers;

[ApiController]
[Route("api/auth/github")]
[EnableRateLimiting("GitHub")]
public class GitHubAuthController : ControllerBase
{
    private readonly IGitHubAuthService _githubAuth;
    private readonly IMediator _mediator;
    private readonly string _frontendUrl;
    private readonly ILogger<GitHubAuthController> _logger;

    public GitHubAuthController(IGitHubAuthService githubAuth, IMediator mediator, IConfiguration configuration, ILogger<GitHubAuthController> logger)
    {
        _githubAuth = githubAuth;
        _mediator = mediator;
        _frontendUrl = configuration["FRONTEND_DOMAIN"] is { Length: > 0 } domain
            ? "https://" + domain
            : "http://localhost:5173";
        _logger = logger;
    }

    [HttpGet("login")]
    public IActionResult Login()
    {
        var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var authUrl = _githubAuth.GetAuthorizationUrl(state);
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
            return Redirect($"{_frontendUrl}/login?error=github_auth_failed");
        }

        var command = new GitHubLoginCommand(
            code,
            state,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            _frontendUrl);

        var outcome = await _mediator.Send(command, ct);

        _logger.LogInformation("GitHub callback outcome: IsSuccess={IsSuccess}, ErrorCode={ErrorCode}, HasAccessToken={HasAccessToken}",
            outcome.Result.IsSuccess, outcome.Result.ErrorCode, outcome.AccessToken != null);

        if (!outcome.Result.IsSuccess)
        {
            _logger.LogWarning("GitHub login failed: {Message}", outcome.Result.ErrorMessage);
            return Redirect($"{_frontendUrl}/login?error=github_auth_failed");
        }

        _logger.LogInformation("GitHub login success, redirecting to /oauth-callback");
        return Redirect(
            $"{_frontendUrl}/oauth-callback#accessToken={Uri.EscapeDataString(outcome.AccessToken)}&refreshToken={Uri.EscapeDataString(outcome.RefreshToken)}&expiresAt={new DateTimeOffset(outcome.ExpiresAt!.Value).ToUnixTimeSeconds()}");
    }
}
