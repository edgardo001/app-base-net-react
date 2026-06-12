using MediatR;
using Microsoft.Extensions.Logging;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Common.Models;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Features.Auth.Commands.GitHubLogin;

public sealed class GitHubLoginCommandHandler : IRequestHandler<GitHubLoginCommand, GitHubLoginOutcome>
{
    private readonly IGitHubAuthService _githubAuth;
    private readonly IUnitOfWork _uow;
    private readonly IJwtService _jwt;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<GitHubLoginCommandHandler> _logger;

    public GitHubLoginCommandHandler(
        IGitHubAuthService githubAuth,
        IUnitOfWork uow,
        IJwtService jwt,
        IDateTimeProvider clock,
        ILogger<GitHubLoginCommandHandler> logger)
    {
        _githubAuth = githubAuth;
        _uow = uow;
        _jwt = jwt;
        _clock = clock;
        _logger = logger;
    }

    public async Task<GitHubLoginOutcome> Handle(GitHubLoginCommand request, CancellationToken ct)
    {
        GitHubUserInfo userInfo;
        try
        {
            userInfo = await _githubAuth.ExchangeCodeAsync(request.Code, request.State, ct);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "GitHub OAuth state validation failed");
            return new GitHubLoginOutcome(
                GitHubLoginResult.Fail(GitHubLoginErrorCode.InvalidState, "Invalid state parameter"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GitHub OAuth token exchange failed");
            return new GitHubLoginOutcome(
                GitHubLoginResult.Fail(GitHubLoginErrorCode.AuthFailed, "GitHub authentication failed"));
        }

        var externalLogin = await _uow.ExternalLogins.GetByProviderAsync("github", userInfo.ProviderId, ct);

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
                var el = ExternalLogin.Create(existingUser.Id, "github", userInfo.ProviderId, userInfo.Email);
                await _uow.ExternalLogins.AddAsync(el, ct);
                user = existingUser;
            }
            else
            {
                user = User.Create(userInfo.Email, userInfo.FirstName, userInfo.LastName, null, null, "github");
                user.ConfirmEmail();
                await _uow.Users.AddAsync(user, ct);
                await _uow.SaveChangesAsync(ct);

                var el = ExternalLogin.Create(user.Id, "github", userInfo.ProviderId, userInfo.Email);
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

        return new GitHubLoginOutcome(
            GitHubLoginResult.Success(),
            accessToken,
            refreshToken,
            expiresAt);
    }
}
