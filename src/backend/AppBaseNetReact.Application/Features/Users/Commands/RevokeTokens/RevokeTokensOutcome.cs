namespace AppBaseNetReact.Application.Features.Users.Commands.RevokeTokens;

public sealed record RevokeTokensOutcome(RevokeTokensResult Result);

public sealed record RevokeTokensResult(bool IsSuccess, string? ErrorCode = null, string? ErrorMessage = null)
{
    public static RevokeTokensResult Success() => new(true);
    public static RevokeTokensResult UserNotFound() => new(false, "UserNotFound", "User not found");
}
