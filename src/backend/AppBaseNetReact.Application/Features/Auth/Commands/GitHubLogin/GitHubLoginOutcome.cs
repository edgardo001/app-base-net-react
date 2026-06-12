namespace AppBaseNetReact.Application.Features.Auth.Commands.GitHubLogin;

public enum GitHubLoginErrorCode
{
    None,
    AuthFailed,
    InvalidState
}

public sealed class GitHubLoginResult
{
    public GitHubLoginErrorCode ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public bool IsSuccess => ErrorCode == GitHubLoginErrorCode.None;

    public static GitHubLoginResult Success() => new() { ErrorCode = GitHubLoginErrorCode.None };

    public static GitHubLoginResult Fail(GitHubLoginErrorCode code, string message) =>
        new() { ErrorCode = code, ErrorMessage = message };
}

public sealed class GitHubLoginOutcome
{
    public GitHubLoginResult Result { get; }
    public string? AccessToken { get; }
    public string? RefreshToken { get; }
    public DateTime? ExpiresAt { get; }

    public GitHubLoginOutcome(GitHubLoginResult result)
    {
        Result = result;
    }

    public GitHubLoginOutcome(GitHubLoginResult result, string accessToken, string refreshToken, DateTime expiresAt)
    {
        Result = result;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresAt = expiresAt;
    }
}
