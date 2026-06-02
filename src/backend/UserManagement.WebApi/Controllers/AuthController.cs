// Sin referencia a Microsoft.EntityFrameworkCore aqui (los controllers
// solo dependen de IUnitOfWork/IJwtService via Application.Interfaces).
// Esto mantiene la WebApi desacoplada del ORM y permite cambiar EF
// Core por Dapper u otro sin tocar los controllers.
// Se usa "sub" en vez de JwtRegisteredClaimNames.Sub porque esa
// clase vive en Microsoft.IdentityModel.JsonWebTokens y requeriria
// agregar el package directamente a WebApi. El claim "sub" es un
// estandar JWT (RFC 7519) que nunca cambia.
using Microsoft.AspNetCore.Mvc;
using UserManagement.Application.Common.Interfaces;
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

    public AuthController(
        IJwtService jwt,
        IPasswordHasherService hasher,
        IUnitOfWork uow,
        IDateTimeProvider clock)
    {
        _jwt = jwt;
        _hasher = hasher;
        _uow = uow;
        _clock = clock;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var user = await _uow.Users.GetByEmailAsync(request.Email, ct);
        if (user == null || !_hasher.VerifyPassword(request.Password, user.PasswordHash))
            return Unauthorized(ApiResponse<object>.Fail("Invalid email or password"));

        if (!user.IsActive)
            return Unauthorized(ApiResponse<object>.Fail("Account is deactivated"));

        if (user.IsLocked())
            return StatusCode(423, ApiResponse<object>.Fail("Account is locked. Try again later."));

        if (!user.EmailConfirmed)
            return StatusCode(403, ApiResponse<object>.Fail("Email not confirmed. Check your inbox."));

        user.MarkLogin();
        await _uow.SaveChangesAsync(ct);

        var permissions = await GetUserPermissions(user.Id, ct);
        var (accessToken, expiresAt) = _jwt.GenerateAccessToken(user, permissions);
        var refreshToken = _jwt.GenerateRefreshToken();
        var tokenHash = _jwt.HashRefreshToken(refreshToken);

        var token = Domain.Entities.RefreshToken.Create(
            user.Id, Guid.NewGuid(), tokenHash,
            _clock.UtcNow.AddDays(7),
            Request.Headers.UserAgent, HttpContext.Connection.RemoteIpAddress?.ToString());

        await _uow.RefreshTokens.AddAsync(token, ct);
        await _uow.SaveChangesAsync(ct);

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
            await _uow.RefreshTokens.RevokeAllForUserAsync(storedToken.UserId, null, ct);
            await _uow.SaveChangesAsync(ct);
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

        var newToken = Domain.Entities.RefreshToken.Create(
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

        if (request.NewPassword != request.ConfirmPassword)
            return BadRequest(ApiResponse<object>.Fail("Passwords do not match"));

        user.SetPasswordHash(_hasher.HashPassword(request.NewPassword));
        await _uow.RefreshTokens.RevokeAllForUserAsync(userId, userId, ct);
        await _uow.SaveChangesAsync(ct);

        return Ok(ApiResponse<object>.Ok(null, "Password changed successfully"));
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
}

public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);
