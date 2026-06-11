namespace AppBaseNetReact.Application.Features.Auth.Commands.GoogleLogin;

public enum GoogleLoginErrorCode
{
    None,
    AuthFailed,
    InvalidState
}

public sealed class GoogleLoginResult
{
    public GoogleLoginErrorCode ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public bool IsSuccess => ErrorCode == GoogleLoginErrorCode.None;

    public static GoogleLoginResult Success() => new() { ErrorCode = GoogleLoginErrorCode.None };

    public static GoogleLoginResult Fail(GoogleLoginErrorCode code, string message) =>
        new() { ErrorCode = code, ErrorMessage = message };
}

public sealed class GoogleLoginOutcome
{
    public GoogleLoginResult Result { get; }
    public string? AccessToken { get; }
    public string? RefreshToken { get; }
    public DateTime? ExpiresAt { get; }

    public GoogleLoginOutcome(GoogleLoginResult result)
    {
        Result = result;
    }

    public GoogleLoginOutcome(GoogleLoginResult result, string accessToken, string refreshToken, DateTime expiresAt)
    {
        Result = result;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresAt = expiresAt;
    }
}
