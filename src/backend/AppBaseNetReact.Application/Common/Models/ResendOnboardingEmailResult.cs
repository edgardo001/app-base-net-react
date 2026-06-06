using AppBaseNetReact.Application.Common.Models;

namespace AppBaseNetReact.Application.Common.Models;

public enum ResendOnboardingErrorCode
{
    None,
    UserNotFound,
    AlreadyConfirmed
}

public sealed class ResendOnboardingEmailResult
{
    public ResendOnboardingErrorCode ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public bool IsSuccess => ErrorCode == ResendOnboardingErrorCode.None;

    public static ResendOnboardingEmailResult Success() => new() { ErrorCode = ResendOnboardingErrorCode.None };

    public static ResendOnboardingEmailResult Fail(ResendOnboardingErrorCode code, string message) =>
        new() { ErrorCode = code, ErrorMessage = message };
}
