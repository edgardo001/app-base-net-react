using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Common.Models;
using AppBaseNetReact.Application.Features.Auth.Commands.Refresh;
using AppBaseNetReact.Application.Features.Auth.Notifications;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Features.Auth.Commands.Refresh;

public sealed class RefreshCommandHandler : IRequestHandler<RefreshCommand, RefreshOutcome>
{
    private const string InvalidTokenMessage = "Invalid refresh token";
    private const string TokenCompromisedMessage = "Token compromised. All sessions revoked.";
    private const string TokenExpiredMessage = "Refresh token expired";
    private const string UserNotFoundOrInactiveMessage = "User not found or inactive";

    private readonly IUnitOfWork _uow;
    private readonly IJwtService _jwt;
    private readonly IDateTimeProvider _clock;
    private readonly IMediator _mediator;

    public RefreshCommandHandler(
        IUnitOfWork uow,
        IJwtService jwt,
        IDateTimeProvider clock,
        IMediator mediator)
    {
        _uow = uow;
        _jwt = jwt;
        _clock = clock;
        _mediator = mediator;
    }

    public async Task<RefreshOutcome> Handle(RefreshCommand request, CancellationToken ct)
    {
        var ip = request.IpAddress ?? "unknown";
        var ua = request.UserAgent ?? "unknown";
        var tokenHash = _jwt.HashRefreshToken(request.RefreshToken);
        var storedToken = await _uow.RefreshTokens.GetByTokenHashAsync(tokenHash, ct);

        if (storedToken == null)
            return new RefreshOutcome(RefreshResult.Fail(RefreshErrorCode.InvalidToken, InvalidTokenMessage), null);

        if (storedToken.IsRevoked)
        {
            await _uow.RefreshTokens.RevokeAllForUserAsync(storedToken.UserId, null, ct);
            await _uow.SaveChangesAsync(ct);

            await _mediator.Publish(
                new TokenReuseDetectedNotification(storedToken.UserId, storedToken.Id, ip, ua), ct);

            return new RefreshOutcome(RefreshResult.Fail(RefreshErrorCode.TokenCompromised, TokenCompromisedMessage), null);
        }

        if (storedToken.IsExpired)
            return new RefreshOutcome(RefreshResult.Fail(RefreshErrorCode.TokenExpired, TokenExpiredMessage), null);

        var user = await _uow.Users.GetByIdWithRolesAsync(storedToken.UserId, ct);
        if (user == null || !user.IsActive)
            return new RefreshOutcome(RefreshResult.Fail(RefreshErrorCode.UserNotFoundOrInactive, UserNotFoundOrInactiveMessage), null);

        storedToken.Revoke(null, tokenHash);

        var permissions = GetUserPermissions(user);
        var (newAccessToken, expiresAt) = _jwt.GenerateAccessToken(user, permissions);
        var newRefreshToken = _jwt.GenerateRefreshToken();
        var newTokenHash = _jwt.HashRefreshToken(newRefreshToken);

        var newToken = RefreshToken.Create(
            user.Id, Guid.NewGuid(), newTokenHash,
            _clock.UtcNow.AddDays(7), ua, ip);

        await _uow.RefreshTokens.AddAsync(newToken, ct);
        await _uow.SaveChangesAsync(ct);

        await _mediator.Publish(new TokenRefreshedNotification(user.Id, user.Email, ip, ua), ct);

        return new RefreshOutcome(
            RefreshResult.Success(),
            new RefreshResponse(newAccessToken, newRefreshToken, expiresAt));
    }

    private static List<string> GetUserPermissions(User user)
    {
        return user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Where(rp => rp.Granted)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToList();
    }
}
