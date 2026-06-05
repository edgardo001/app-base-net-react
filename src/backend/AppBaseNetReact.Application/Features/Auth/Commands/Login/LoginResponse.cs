namespace AppBaseNetReact.Application.Features.Auth.Commands.Login;

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string? AvatarPath,
    IReadOnlyList<string> Permissions,
    bool PasswordExpired);
