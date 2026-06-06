using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using AppBaseNetReact.Application.Common.Models;
using AppBaseNetReact.Application.Common.Validators;
using AppBaseNetReact.Application.Features.Auth.Commands.ChangePassword;
using AppBaseNetReact.Application.Features.Auth.Commands.ConfirmEmail;
using AppBaseNetReact.Application.Features.Auth.Commands.ForgotPassword;
using AppBaseNetReact.Application.Features.Auth.Commands.Login;
using AppBaseNetReact.Application.Features.Auth.Commands.Logout;
using AppBaseNetReact.Application.Features.Auth.Commands.Refresh;
using AppBaseNetReact.Application.Features.Auth.Commands.ResetPassword;
using AppBaseNetReact.WebApi.Filters;

namespace AppBaseNetReact.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly string _frontendUrl;

    public AuthController(IMediator mediator, IConfiguration configuration)
    {
        _mediator = mediator;
        _frontendUrl = configuration["FrontendUrl"] ?? "http://localhost:5173";
    }

    [HttpPost("login")]
    [EnableRateLimiting("Login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var command = new LoginCommand(
            request.Email,
            request.Password,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            _frontendUrl);

        var outcome = await _mediator.Send(command, ct);

        if (outcome.Result.IsSuccess)
        {
            var r = outcome.Response!;
            return Ok(ApiResponse<object>.Ok(new
            {
                AccessToken = r.AccessToken,
                RefreshToken = r.RefreshToken,
                ExpiresAt = r.ExpiresAt,
                User = new { r.UserId, r.Email, r.FirstName, r.LastName, r.AvatarPath },
                Permissions = r.Permissions,
                PasswordExpired = r.PasswordExpired
            }));
        }

        return outcome.Result.ErrorCode switch
        {
            LoginErrorCode.AccountLocked => StatusCode(423, ApiResponse<object>.Fail(outcome.Result.ErrorMessage!)),
            LoginErrorCode.EmailNotConfirmed => StatusCode(403, ApiResponse<object>.Fail(outcome.Result.ErrorMessage!)),
            _ => Unauthorized(ApiResponse<object>.Fail(outcome.Result.ErrorMessage!))
        };
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var command = new RefreshCommand(
            request.RefreshToken,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        var outcome = await _mediator.Send(command, ct);

        if (outcome.Result.IsSuccess)
        {
            var r = outcome.Response!;
            return Ok(ApiResponse<object>.Ok(new
            {
                AccessToken = r.AccessToken,
                RefreshToken = r.RefreshToken,
                ExpiresAt = r.ExpiresAt
            }));
        }

        return Unauthorized(ApiResponse<object>.Fail(outcome.Result.ErrorMessage!));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var command = new LogoutCommand(
            request.RefreshToken,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        await _mediator.Send(command, ct);

        return Ok(ApiResponse<object>.Ok("Logged out successfully"));
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var command = new ChangePasswordCommand(
            userId,
            request.CurrentPassword,
            request.NewPassword,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        var outcome = await _mediator.Send(command, ct);

        return outcome.Result.ErrorCode switch
        {
            PasswordErrorCode.None => Ok(ApiResponse<object>.Ok("Password changed successfully")),
            PasswordErrorCode.UserNotFound => NotFound(),
            _ => BadRequest(ApiResponse<object>.Fail(outcome.Result.ErrorMessage!))
        };
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("ForgotPassword")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        var command = new ForgotPasswordCommand(
            request.Email,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        await _mediator.Send(command, ct);

        return Ok(ApiResponse<object>.Ok("If the email exists, a password reset link has been sent."));
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var command = new ResetPasswordCommand(
            request.Token,
            request.NewPassword,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        var outcome = await _mediator.Send(command, ct);

        return outcome.Result.ErrorCode switch
        {
            PasswordErrorCode.None => Ok(ApiResponse<object>.Ok("Password reset successfully")),
            _ => BadRequest(ApiResponse<object>.Fail(outcome.Result.ErrorMessage!))
        };
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request, CancellationToken ct)
    {
        var loginLink = $"{Request.Scheme}://{Request.Host}/login";
        var command = new ConfirmEmailCommand(
            request.Token,
            loginLink,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        var outcome = await _mediator.Send(command, ct);

        return outcome.Result.ErrorCode switch
        {
            EmailErrorCode.None => Ok(ApiResponse<object>.Ok("Email confirmed successfully")),
            _ => BadRequest(ApiResponse<object>.Fail(outcome.Result.ErrorMessage!))
        };
    }
}

// Types defined in AppBaseNetReact.Application.Common.Validators
