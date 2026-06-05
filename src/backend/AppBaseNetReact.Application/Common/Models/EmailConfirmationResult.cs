namespace AppBaseNetReact.Application.Common.Models;

public enum EmailErrorCode
{
    None,
    InvalidConfirmationToken,
    ConfirmationTokenExpired,
    UserNotFound
}

public sealed class EmailConfirmationResult
{
    public EmailErrorCode ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public bool IsSuccess => ErrorCode == EmailErrorCode.None;

    public static EmailConfirmationResult Success() => new() { ErrorCode = EmailErrorCode.None };

    public static EmailConfirmationResult Fail(EmailErrorCode code, string message) =>
        new() { ErrorCode = code, ErrorMessage = message };
}
