using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Common.Validators;
using AppBaseNetReact.Domain.Entities;
using AppBaseNetReact.Infrastructure.Email;
using AppBaseNetReact.Infrastructure.Services;
using AppBaseNetReact.WebApi.Filters;
using Serilog;

namespace AppBaseNetReact.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IJwtService _jwt;
    private readonly IPasswordHasherService _hasher;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _audit;
    private readonly IPasswordPolicyService _passwordPolicy;
    private readonly IEmailService _email;
    private readonly EmailRenderer _renderer;
    private readonly EmailOptions _emailOptions;
    private readonly string _frontendUrl;

    public AuthController(
        IJwtService jwt,
        IPasswordHasherService hasher,
        IUnitOfWork uow,
        IDateTimeProvider clock,
        IAuditService audit,
        IPasswordPolicyService passwordPolicy,
        IEmailService email,
        EmailRenderer renderer,
        IOptions<EmailOptions> emailOptions,
        IConfiguration configuration)
    {
        _jwt = jwt;
        _hasher = hasher;
        _uow = uow;
        _clock = clock;
        _audit = audit;
        _passwordPolicy = passwordPolicy;
        _email = email;
        _renderer = renderer;
        _emailOptions = emailOptions.Value;
        _frontendUrl = configuration["FrontendUrl"] ?? "http://localhost:5173";
    }

    [HttpPost("login")]
    [EnableRateLimiting("Login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var user = await _uow.Users.GetByEmailAsync(request.Email, ct);

        if (user == null || !_hasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            if (user != null)
            {
                user.IncrementFailedAccess();
                if (user.AccessFailedCount >= _passwordPolicy.MaxFailedAccessAttempts)
                {
                    user.LockUntil(_clock.UtcNow.AddMinutes(_passwordPolicy.DefaultLockoutMinutes));
                    await _uow.SaveChangesAsync(ct);
                    await SendAccountLockedEmail(user, ct);
                }
                else
                {
                    await _uow.SaveChangesAsync(ct);
                }
            }

            await LogLoginAttempt(request.Email, false, "Invalid credentials", ct);
            return Unauthorized(ApiResponse<object>.Fail("Invalid email or password"));
        }

        if (!user.IsActive)
        {
            await LogLoginAttempt(request.Email, false, "Account deactivated", ct);
            return Unauthorized(ApiResponse<object>.Fail("Account is deactivated"));
        }

        if (user.IsLocked())
        {
            var remaining = (user.LockoutEnd!.Value - _clock.UtcNow).Minutes;
            await LogLoginAttempt(request.Email, false, "Account locked", ct);
            return StatusCode(423, ApiResponse<object>.Fail($"Account is locked. Try again in {remaining} minutes."));
        }

        if (!user.EmailConfirmed)
        {
            await LogLoginAttempt(request.Email, false, "Email not confirmed", ct);
            return StatusCode(403, ApiResponse<object>.Fail("Email not confirmed. Check your inbox."));
        }

        user.MarkLogin();
        await _uow.SaveChangesAsync(ct);

        var permissions = await GetUserPermissions(user.Id, ct);
        var (accessToken, expiresAt) = _jwt.GenerateAccessToken(user, permissions);
        var refreshToken = _jwt.GenerateRefreshToken();
        var tokenHash = _jwt.HashRefreshToken(refreshToken);

        var token = RefreshToken.Create(
            user.Id, Guid.NewGuid(), tokenHash,
            _clock.UtcNow.AddDays(7),
            Request.Headers.UserAgent, HttpContext.Connection.RemoteIpAddress?.ToString());

        await _uow.RefreshTokens.AddAsync(token, ct);
        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "UserLoggedIn", "User", user.Id.ToString(),
            null, null, user.Id,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers.UserAgent.ToString(),
            $"User {user.Email} logged in", ct);

        return Ok(ApiResponse<object>.Ok(new
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            User = new { user.Id, user.Email, user.FirstName, user.LastName, user.AvatarPath },
            Permissions = permissions,
            PasswordExpired = user.IsPasswordExpired()
        }));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var tokenHash = _jwt.HashRefreshToken(request.RefreshToken);
        var storedToken = await _uow.RefreshTokens.GetByTokenHashAsync(tokenHash, ct);

        if (storedToken == null)
            return Unauthorized(ApiResponse<object>.Fail("Invalid refresh token"));

        if (storedToken.IsRevoked)
        {
            // Token reuse detection — revoke all sessions for this user
            await _uow.RefreshTokens.RevokeAllForUserAsync(storedToken.UserId, null, ct);
            await _uow.SaveChangesAsync(ct);

            await _audit.LogAsync(
                "TokenReuseDetected", "RefreshToken", storedToken.Id.ToString(),
                null, null, storedToken.UserId,
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                Request.Headers.UserAgent.ToString(),
                "Compromised refresh token detected — all sessions revoked", ct);

            return Unauthorized(ApiResponse<object>.Fail("Token compromised. All sessions revoked."));
        }

        if (storedToken.IsExpired)
            return Unauthorized(ApiResponse<object>.Fail("Refresh token expired"));

        var user = await _uow.Users.GetByIdWithRolesAsync(storedToken.UserId, ct);
        if (user == null || !user.IsActive)
            return Unauthorized(ApiResponse<object>.Fail("User not found or inactive"));

        storedToken.Revoke(null, tokenHash);

        var permissions = await GetUserPermissions(user.Id, ct);
        var (newAccessToken, expiresAt) = _jwt.GenerateAccessToken(user, permissions);
        var newRefreshToken = _jwt.GenerateRefreshToken();
        var newTokenHash = _jwt.HashRefreshToken(newRefreshToken);

        var newToken = RefreshToken.Create(
            user.Id, Guid.NewGuid(), newTokenHash,
            _clock.UtcNow.AddDays(7),
            Request.Headers.UserAgent, HttpContext.Connection.RemoteIpAddress?.ToString());

        await _uow.RefreshTokens.AddAsync(newToken, ct);
        await _uow.SaveChangesAsync(ct);

        return Ok(ApiResponse<object>.Ok(new
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = expiresAt
        }));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var tokenHash = _jwt.HashRefreshToken(request.RefreshToken);
        var storedToken = await _uow.RefreshTokens.GetByTokenHashAsync(tokenHash, ct);

        if (storedToken != null)
        {
            storedToken.Revoke();

            await _audit.LogAsync(
                "UserLoggedOut", "RefreshToken", storedToken.Id.ToString(),
                null, null, storedToken.UserId,
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                Request.Headers.UserAgent.ToString(), null, ct);

            await _uow.SaveChangesAsync(ct);
        }

        return Ok(ApiResponse<object>.Ok(null, "Logged out successfully"));
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _uow.Users.GetByIdAsync(userId, ct);
        if (user == null)
            return NotFound();

        if (!_hasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            return BadRequest(ApiResponse<object>.Fail("Current password is incorrect"));

        var (valid, error) = _passwordPolicy.Validate(request.NewPassword);
        if (!valid)
            return BadRequest(ApiResponse<object>.Fail(error));

        user.SetPasswordHash(_hasher.HashPassword(request.NewPassword));
        await _uow.RefreshTokens.RevokeAllForUserAsync(userId, userId, ct);
        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "PasswordChanged", "User", user.Id.ToString(),
            null, null, userId,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers.UserAgent.ToString(), null, ct);

        try
        {
            await SendEmail(user, "PasswordChanged", new Dictionary<string, string>
            {
                ["UserName"] = user.FirstName
            }, ct);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to send password change email for user {UserId}", userId);
        }

        return Ok(ApiResponse<object>.Ok(null, "Password changed successfully"));
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("ForgotPassword")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        var user = await _uow.Users.GetByEmailAsync(request.Email, ct);
        // Always return success to prevent email enumeration
        if (user == null)
            return Ok(ApiResponse<object>.Ok(null, "If the email exists, a password reset link has been sent."));

        var resetToken = GenerateToken();
        user.SetEmailConfirmationToken(resetToken, _clock.UtcNow.AddHours(24));
        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "PasswordResetRequested", "User", user.Id.ToString(),
            null, null, user.Id,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers.UserAgent.ToString(), "Reset token generated", ct);

        var resetLink = $"{_frontendUrl}/reset-password?token={resetToken}";

        await SendEmail(user, "PasswordReset", new Dictionary<string, string>
        {
            ["UserName"] = user.FirstName,
            ["ResetLink"] = resetLink
        }, ct);

        return Ok(ApiResponse<object>.Ok(null, "If the email exists, a password reset link has been sent."));
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var user = await _uow.Users.GetByEmailConfirmationTokenAsync(request.Token, ct);
        if (user == null)
            return BadRequest(ApiResponse<object>.Fail("Invalid reset token"));

        if (user.EmailConfirmationTokenExpires < _clock.UtcNow)
            return BadRequest(ApiResponse<object>.Fail("Reset token has expired"));

        var (valid, error) = _passwordPolicy.Validate(request.NewPassword);
        if (!valid)
            return BadRequest(ApiResponse<object>.Fail(error));

        user.SetPasswordHash(_hasher.HashPassword(request.NewPassword));
        user.ForcePasswordChange();
        user.ConfirmEmail();
        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "PasswordReset", "User", user.Id.ToString(),
            null, null, user.Id,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers.UserAgent.ToString(), "Password reset via token", ct);

        await SendEmail(user, "PasswordChanged", new Dictionary<string, string>
        {
            ["UserName"] = user.FirstName
        }, ct);

        return Ok(ApiResponse<object>.Ok(null, "Password reset successfully"));
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request, CancellationToken ct)
    {
        var user = await _uow.Users.GetByEmailConfirmationTokenAsync(request.Token, ct);
        if (user == null)
            return BadRequest(ApiResponse<object>.Fail("Invalid confirmation token"));

        if (user.EmailConfirmationTokenExpires < _clock.UtcNow)
            return BadRequest(ApiResponse<object>.Fail("Confirmation token has expired"));

        user.ConfirmEmail();
        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "EmailConfirmed", "User", user.Id.ToString(),
            null, null, user.Id,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers.UserAgent.ToString(), null, ct);

        await SendEmail(user, "Welcome", new Dictionary<string, string>
        {
            ["UserName"] = user.FirstName,
            ["LoginLink"] = $"{Request.Scheme}://{Request.Host}/login"
        }, ct);

        return Ok(ApiResponse<object>.Ok(null, "Email confirmed successfully"));
    }

    private async Task<List<string>> GetUserPermissions(Guid userId, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdWithRolesAsync(userId, ct);
        if (user == null) return [];

        return user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Where(rp => rp.Granted)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToList();
    }

    private async Task LogLoginAttempt(string email, bool success, string? reason, CancellationToken ct)
    {
        var attempt = LoginAttempt.Create(
            email,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            success, reason);

        await _uow.LoginAttempts.AddAsync(attempt, ct);
        await _uow.SaveChangesAsync(ct);
    }

    private async Task SendEmail(Domain.Entities.User user, string templateName, Dictionary<string, string> extraVars, CancellationToken ct)
    {
        if (!_emailOptions.Templates.TryGetValue(templateName, out var config)) return;

        var vars = new Dictionary<string, string>(extraVars)
        {
            ["Year"] = DateTime.UtcNow.Year.ToString()
        };

        var htmlBody = _renderer.Render(config.TemplateFile, vars);
        await _email.SendEmailAsync(user.Email, config.Subject, htmlBody, ct);
    }

    private async Task SendAccountLockedEmail(Domain.Entities.User user, CancellationToken ct)
    {
        await SendEmail(user, "AccountLocked", new Dictionary<string, string>
        {
            ["UserName"] = user.FirstName,
            ["LockoutMinutes"] = _passwordPolicy.DefaultLockoutMinutes.ToString(),
            ["ResetLink"] = $"{_frontendUrl}/reset-password"
        }, ct);
    }

    private static string GenerateToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    }
}

// Types defined in AppBaseNetReact.Application.Common.Validators
