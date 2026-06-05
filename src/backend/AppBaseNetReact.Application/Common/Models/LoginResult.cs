namespace AppBaseNetReact.Application.Common.Models;

public enum LoginErrorCode
{
    None,
    InvalidCredentials,
    AccountDeactivated,
    AccountLocked,
    EmailNotConfirmed
}

public sealed class LoginResult
{
    public LoginErrorCode ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public int? RemainingLockoutMinutes { get; init; }

    public bool IsSuccess => ErrorCode == LoginErrorCode.None;

    public static LoginResult Success() => new() { ErrorCode = LoginErrorCode.None };

    public static LoginResult Fail(LoginErrorCode code, string message, int? remainingLockoutMinutes = null) =>
        new() { ErrorCode = code, ErrorMessage = message, RemainingLockoutMinutes = remainingLockoutMinutes };
}
