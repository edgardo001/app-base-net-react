using MediatR;

namespace AppBaseNetReact.Application.Features.Auth.Notifications;

public sealed record UserLoggedInNotification(
    Guid UserId,
    string Email,
    string IpAddress,
    string UserAgent) : INotification;

public sealed record UserLoginFailedNotification(
    string Email,
    string IpAddress,
    string Reason) : INotification;

public sealed record AccountLockedNotification(
    Guid UserId,
    string Email,
    string FirstName,
    string IpAddress,
    int LockoutMinutes,
    string FrontendUrl) : INotification;

public sealed record TokenReuseDetectedNotification(
    Guid UserId,
    Guid RefreshTokenId,
    string IpAddress,
    string UserAgent) : INotification;

public sealed record TokenRefreshedNotification(
    Guid UserId,
    string Email,
    string IpAddress,
    string UserAgent) : INotification;

public sealed record UserLoggedOutNotification(
    Guid UserId,
    string IpAddress,
    string UserAgent) : INotification;

public sealed record PasswordChangedNotification(
    Guid UserId,
    string Email,
    string FirstName,
    string IpAddress,
    string UserAgent) : INotification;

public sealed record PasswordResetRequestedNotification(
    Guid UserId,
    string Email,
    string FirstName,
    string ResetLink,
    string IpAddress,
    string UserAgent) : INotification;

public sealed record PasswordResetNotification(
    Guid UserId,
    string Email,
    string FirstName,
    string IpAddress,
    string UserAgent) : INotification;

public sealed record EmailConfirmedNotification(
    Guid UserId,
    string Email,
    string FirstName,
    string IpAddress,
    string UserAgent,
    string LoginLink) : INotification;

public sealed record OnboardingEmailResentNotification(
    Guid UserId,
    string Email,
    string FirstName,
    string NewConfirmationToken,
    string IpAddress,
    string UserAgent) : INotification;
