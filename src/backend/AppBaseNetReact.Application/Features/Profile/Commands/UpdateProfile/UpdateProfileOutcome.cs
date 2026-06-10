namespace AppBaseNetReact.Application.Features.Profile.Commands.UpdateProfile;

public sealed record UpdateProfileOutcome(UpdateProfileResult Result)
{
    public static UpdateProfileOutcome Success() => new(UpdateProfileResult.Success());
    public static UpdateProfileOutcome UserNotFound() => new(UpdateProfileResult.UserNotFound());
    public static UpdateProfileOutcome Unauthorized() => new(UpdateProfileResult.Unauthorized());
}

public sealed record UpdateProfileResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorCode { get; init; }

    private UpdateProfileResult() { }

    public static UpdateProfileResult Success() => new() { IsSuccess = true };
    public static UpdateProfileResult UserNotFound() => new() { ErrorCode = "UserNotFound" };
    public static UpdateProfileResult Unauthorized() => new() { ErrorCode = "Unauthorized" };
}
