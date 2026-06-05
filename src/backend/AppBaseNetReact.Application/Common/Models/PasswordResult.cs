namespace AppBaseNetReact.Application.Common.Models;

public enum PasswordErrorCode
{
    None,
    InvalidCurrentPassword,
    WeakPassword,
    InvalidResetToken,
    ResetTokenExpired,
    UserNotFound
}

public sealed class PasswordResult
{
    public PasswordErrorCode ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public bool IsSuccess => ErrorCode == PasswordErrorCode.None;

    public static PasswordResult Success() => new() { ErrorCode = PasswordErrorCode.None };

    public static PasswordResult Fail(PasswordErrorCode code, string message) =>
        new() { ErrorCode = code, ErrorMessage = message };
}
