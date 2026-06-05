using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Common.Models;
using AppBaseNetReact.Application.Features.Auth.Commands.Login;
using AppBaseNetReact.Application.Features.Auth.Notifications;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginOutcome>
{
    private const string InvalidCredentialsMessage = "Invalid email or password";
    private const string AccountDeactivatedMessage = "Account is deactivated";
    private const string AccountLockedPrefix = "Account is locked. Try again in";
    private const string EmailNotConfirmedMessage = "Email not confirmed. Check your inbox.";

    private readonly IUnitOfWork _uow;
    private readonly IJwtService _jwt;
    private readonly IPasswordHasherService _hasher;
    private readonly IPasswordPolicyService _passwordPolicy;
    private readonly IDateTimeProvider _clock;
    private readonly IMediator _mediator;

    public LoginCommandHandler(
        IUnitOfWork uow,
        IJwtService jwt,
        IPasswordHasherService hasher,
        IPasswordPolicyService passwordPolicy,
        IDateTimeProvider clock,
        IMediator mediator)
    {
        _uow = uow;
        _jwt = jwt;
        _hasher = hasher;
        _passwordPolicy = passwordPolicy;
        _clock = clock;
        _mediator = mediator;
    }

    public async Task<LoginOutcome> Handle(LoginCommand request, CancellationToken ct)
    {
        var ip = request.IpAddress ?? "unknown";
        var ua = request.UserAgent ?? "unknown";
        var user = await _uow.Users.GetByEmailAsync(request.Email, ct);

        if (user == null || !_hasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            if (user != null)
            {
                user.IncrementFailedAccess();
                var shouldLock = user.AccessFailedCount >= _passwordPolicy.MaxFailedAccessAttempts;
                if (shouldLock)
                {
                    user.LockUntil(_clock.UtcNow.AddMinutes(_passwordPolicy.DefaultLockoutMinutes));
                }
                await _uow.SaveChangesAsync(ct);

                if (shouldLock)
                {
                    await _mediator.Publish(new AccountLockedNotification(
                        user.Id, user.Email, user.FirstName, ip,
                        _passwordPolicy.DefaultLockoutMinutes,
                        request.FrontendUrl ?? "http://localhost:5173"), ct);
                }
            }

            await PersistLoginAttemptAsync(request.Email, ip, false, "Invalid credentials", ct);
            await _mediator.Publish(new UserLoginFailedNotification(request.Email, ip, "Invalid credentials"), ct);
            return new LoginOutcome(LoginResult.Fail(LoginErrorCode.InvalidCredentials, InvalidCredentialsMessage), null);
        }

        if (!user.IsActive)
        {
            await PersistLoginAttemptAsync(request.Email, ip, false, "Account deactivated", ct);
            await _mediator.Publish(new UserLoginFailedNotification(request.Email, ip, "Account deactivated"), ct);
            return new LoginOutcome(LoginResult.Fail(LoginErrorCode.AccountDeactivated, AccountDeactivatedMessage), null);
        }

        if (user.IsLocked())
        {
            var remaining = (int)Math.Ceiling((user.LockoutEnd!.Value - _clock.UtcNow).TotalMinutes);
            if (remaining < 1) remaining = 1;
            await PersistLoginAttemptAsync(request.Email, ip, false, "Account locked", ct);
            await _mediator.Publish(new UserLoginFailedNotification(request.Email, ip, "Account locked"), ct);
            return new LoginOutcome(
                LoginResult.Fail(LoginErrorCode.AccountLocked, $"{AccountLockedPrefix} {remaining} minutes.", remaining),
                null);
        }

        if (!user.EmailConfirmed)
        {
            await PersistLoginAttemptAsync(request.Email, ip, false, "Email not confirmed", ct);
            await _mediator.Publish(new UserLoginFailedNotification(request.Email, ip, "Email not confirmed"), ct);
            return new LoginOutcome(LoginResult.Fail(LoginErrorCode.EmailNotConfirmed, EmailNotConfirmedMessage), null);
        }

        user.MarkLogin();
        await _uow.SaveChangesAsync(ct);

        var permissions = GetUserPermissions(user);
        var (accessToken, expiresAt) = _jwt.GenerateAccessToken(user, permissions);
        var refreshToken = _jwt.GenerateRefreshToken();
        var tokenHash = _jwt.HashRefreshToken(refreshToken);

        var refresh = RefreshToken.Create(
            user.Id, Guid.NewGuid(), tokenHash,
            _clock.UtcNow.AddDays(7),
            ua, ip);

        await _uow.RefreshTokens.AddAsync(refresh, ct);
        await _uow.SaveChangesAsync(ct);

        await _mediator.Publish(new UserLoggedInNotification(user.Id, user.Email, ip, ua), ct);

        var response = new LoginResponse(
            accessToken,
            refreshToken,
            expiresAt,
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.AvatarPath,
            permissions,
            user.IsPasswordExpired());

        return new LoginOutcome(LoginResult.Success(), response);
    }

    private async Task PersistLoginAttemptAsync(string email, string ip, bool success, string reason, CancellationToken ct)
    {
        var attempt = LoginAttempt.Create(email, ip, success, reason);
        await _uow.LoginAttempts.AddAsync(attempt, ct);
        await _uow.SaveChangesAsync(ct);
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
