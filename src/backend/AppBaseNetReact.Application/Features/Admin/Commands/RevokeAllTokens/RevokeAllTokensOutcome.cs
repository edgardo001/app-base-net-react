namespace AppBaseNetReact.Application.Features.Admin.Commands.RevokeAllTokens;

public sealed record RevokeAllTokensOutcome(RevokeAllTokensResult Result)
{
    public static RevokeAllTokensOutcome Success() => new(RevokeAllTokensResult.Success());
}

public sealed record RevokeAllTokensResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorCode { get; init; }

    private RevokeAllTokensResult() { }

    public static RevokeAllTokensResult Success() => new() { IsSuccess = true };
}
