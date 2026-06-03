using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using UserManagement.Application.Common.Interfaces;
using UserManagement.Application.Common.Validators;
using UserManagement.Domain.Entities;
using UserManagement.WebApi.Filters;

namespace UserManagement.WebApi.Controllers;

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

    public AuthController(
        IJwtService jwt,
        IPasswordHasherService hasher,
        IUnitOfWork uow,
        IDateTimeProvider clock,
        IAuditService audit,
        IPasswordPolicyService passwordPolicy)
    {
        _jwt = jwt;
        _hasher = hasher;
        _uow = uow;
        _clock = clock;
        _audit = audit;
        _passwordPolicy = passwordPolicy;
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
                    user.LockUntil(_clock.UtcNow.AddMinutes(_passwordPolicy.DefaultLockoutMinutes));
                await _uow.SaveChangesAsync(ct);
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

        var tempPassword = Guid.NewGuid().ToString("N")[..12];
        user.SetPasswordHash(_hasher.HashPassword(tempPassword));
        user.ForcePasswordChange();
        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "PasswordResetRequested", "User", user.Id.ToString(),
            null, null, user.Id,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers.UserAgent.ToString(), "Temporary password generated", ct);

        // TODO: Send email with temp password when EmailService is configured

        return Ok(ApiResponse<object>.Ok(
            new { TemporaryPassword = tempPassword },
            "If the email exists, a password reset link has been sent."));
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
}

// Types defined in UserManagement.Application.Common.Validators
