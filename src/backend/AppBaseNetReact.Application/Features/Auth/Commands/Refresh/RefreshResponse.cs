namespace AppBaseNetReact.Application.Features.Auth.Commands.Refresh;

public sealed record RefreshResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt);
