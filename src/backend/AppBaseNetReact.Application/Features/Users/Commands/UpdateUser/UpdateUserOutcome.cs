namespace AppBaseNetReact.Application.Features.Users.Commands.UpdateUser;

public sealed record UpdateUserOutcome(UpdateUserResult Result);

public sealed record UpdateUserResult(bool IsSuccess, string? ErrorCode = null, string? ErrorMessage = null)
{
    public static UpdateUserResult Success() => new(true);
    public static UpdateUserResult UserNotFound() => new(false, "UserNotFound", "User not found");
}
