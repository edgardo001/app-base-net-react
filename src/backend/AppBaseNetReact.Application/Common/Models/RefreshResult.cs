namespace AppBaseNetReact.Application.Common.Models;

public enum RefreshErrorCode
{
    None,
    InvalidToken,
    TokenCompromised,
    TokenExpired,
    UserNotFoundOrInactive
}

public sealed class RefreshResult
{
    public RefreshErrorCode ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public bool IsSuccess => ErrorCode == RefreshErrorCode.None;

    public static RefreshResult Success() => new() { ErrorCode = RefreshErrorCode.None };

    public static RefreshResult Fail(RefreshErrorCode code, string message) =>
        new() { ErrorCode = code, ErrorMessage = message };
}
