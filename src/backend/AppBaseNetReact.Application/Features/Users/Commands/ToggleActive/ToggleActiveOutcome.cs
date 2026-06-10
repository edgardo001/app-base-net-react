namespace AppBaseNetReact.Application.Features.Users.Commands.ToggleActive;

public sealed record ToggleActiveOutcome(ToggleActiveResult Result);

public sealed record ToggleActiveResult(bool IsSuccess, bool? IsActive = null, string? ErrorCode = null, string? ErrorMessage = null)
{
    public static ToggleActiveResult Success(bool isActive) => new(true, isActive);
    public static ToggleActiveResult UserNotFound() => new(false, null, "UserNotFound", "User not found");
}
