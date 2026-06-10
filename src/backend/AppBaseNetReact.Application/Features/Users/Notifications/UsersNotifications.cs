using MediatR;

namespace AppBaseNetReact.Application.Features.Users.Notifications;

public sealed record UserCreatedNotification(
    Guid UserId,
    string Email,
    string FirstName,
    string ConfirmationToken,
    string TemporaryPassword,
    string FrontendBaseUrl,
    string IpAddress,
    string UserAgent) : INotification;

public sealed record UserUpdatedNotification(
    Guid UserId,
    string Email,
    string IpAddress,
    string UserAgent) : INotification;

public sealed record UserDeletedNotification(
    Guid UserId,
    string Email,
    Guid? DeletedBy,
    string IpAddress,
    string UserAgent) : INotification;

public sealed record UserActivatedNotification(
    Guid UserId,
    string Email,
    bool IsActive,
    string IpAddress,
    string UserAgent) : INotification;

public sealed record UserDeactivatedNotification(
    Guid UserId,
    string Email,
    bool IsActive,
    string IpAddress,
    string UserAgent) : INotification;

public sealed record PasswordResetByAdminNotification(
    Guid UserId,
    string Email,
    string FirstName,
    string TemporaryPassword,
    string LoginLink,
    string IpAddress,
    string UserAgent) : INotification;

public sealed record TokensRevokedNotification(
    Guid UserId,
    string IpAddress,
    string UserAgent) : INotification;

public sealed record AvatarUpdatedNotification(
    Guid UserId,
    string Email,
    string FileName,
    string IpAddress,
    string UserAgent) : INotification;
