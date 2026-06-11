using MediatR;
using Microsoft.Extensions.Logging;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Common.Models;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Features.Auth.Commands.GoogleLogin;

public sealed class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, GoogleLoginOutcome>
{
    private readonly IGoogleAuthService _googleAuth;
    private readonly IUnitOfWork _uow;
    private readonly IJwtService _jwt;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<GoogleLoginCommandHandler> _logger;

    public GoogleLoginCommandHandler(
        IGoogleAuthService googleAuth,
        IUnitOfWork uow,
        IJwtService jwt,
        IDateTimeProvider clock,
        ILogger<GoogleLoginCommandHandler> logger)
    {
        _googleAuth = googleAuth;
        _uow = uow;
        _jwt = jwt;
        _clock = clock;
        _logger = logger;
    }

    public async Task<GoogleLoginOutcome> Handle(GoogleLoginCommand request, CancellationToken ct)
    {
        GoogleUserInfo userInfo;
        try
        {
            userInfo = await _googleAuth.ExchangeCodeAsync(request.Code, request.State, ct);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Google OAuth state validation failed");
            return new GoogleLoginOutcome(
                GoogleLoginResult.Fail(GoogleLoginErrorCode.InvalidState, "Invalid state parameter"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google OAuth token exchange failed");
            return new GoogleLoginOutcome(
                GoogleLoginResult.Fail(GoogleLoginErrorCode.AuthFailed, "Google authentication failed"));
        }

        var externalLogin = await _uow.ExternalLogins.GetByProviderAsync("google", userInfo.ProviderId, ct);

        User user;

        if (externalLogin != null)
        {
            user = externalLogin.User;
        }
        else
        {
            var existingUser = await _uow.Users.GetByEmailAsync(userInfo.Email, ct);

            if (existingUser != null)
            {
                var el = ExternalLogin.Create(existingUser.Id, "google", userInfo.ProviderId, userInfo.Email);
                await _uow.ExternalLogins.AddAsync(el, ct);
                user = existingUser;
            }
            else
            {
                user = User.Create(userInfo.Email, userInfo.FirstName, userInfo.LastName, null, null, "google");
                user.ConfirmEmail();
                await _uow.Users.AddAsync(user, ct);
                await _uow.SaveChangesAsync(ct);

                var el = ExternalLogin.Create(user.Id, "google", userInfo.ProviderId, userInfo.Email);
                await _uow.ExternalLogins.AddAsync(el, ct);
            }
        }

        var publicRole = await _uow.Roles.GetByNameAsync("public", ct);
        if (publicRole != null && user.UserRoles.All(ur => ur.RoleId != publicRole.Id))
        {
            user.UserRoles.Add(UserRole.Create(user.Id, publicRole.Id, publicRole));
        }

        user.MarkLogin();
        await _uow.SaveChangesAsync(ct);

        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Where(rp => rp.Granted)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToList();

        var (accessToken, expiresAt) = _jwt.GenerateAccessToken(user, permissions);
        var refreshToken = _jwt.GenerateRefreshToken();
        var tokenHash = _jwt.HashRefreshToken(refreshToken);

        var refresh = RefreshToken.Create(
            user.Id, Guid.NewGuid(), tokenHash,
            _clock.UtcNow.AddDays(7),
            request.UserAgent ?? "unknown",
            request.IpAddress ?? "unknown");

        await _uow.RefreshTokens.AddAsync(refresh, ct);
        await _uow.SaveChangesAsync(ct);

        return new GoogleLoginOutcome(
            GoogleLoginResult.Success(),
            accessToken,
            refreshToken,
            expiresAt);
    }
}
