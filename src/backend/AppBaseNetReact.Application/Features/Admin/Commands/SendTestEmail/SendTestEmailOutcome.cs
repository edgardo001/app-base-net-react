namespace AppBaseNetReact.Application.Features.Admin.Commands.SendTestEmail;

public sealed record SendTestEmailOutcome(SendTestEmailResult Result)
{
    public static SendTestEmailOutcome Success() => new(SendTestEmailResult.Success());
    public static SendTestEmailOutcome Failed(string error) => new(SendTestEmailResult.Failed(error));
}

public sealed record SendTestEmailResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    private SendTestEmailResult() { }

    public static SendTestEmailResult Success() => new() { IsSuccess = true };
    public static SendTestEmailResult Failed(string error) => new() { ErrorCode = "SendFailed", ErrorMessage = error };
}
