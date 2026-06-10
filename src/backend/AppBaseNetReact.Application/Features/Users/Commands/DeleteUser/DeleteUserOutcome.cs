namespace AppBaseNetReact.Application.Features.Users.Commands.DeleteUser;

public sealed record DeleteUserOutcome(DeleteUserResult Result);

public sealed record DeleteUserResult(bool IsSuccess, string? ErrorCode = null, string? ErrorMessage = null)
{
    public static DeleteUserResult Success() => new(true);
    public static DeleteUserResult UserNotFound() => new(false, "UserNotFound", "User not found");
    public static DeleteUserResult CannotDeleteSelf() => new(false, "CannotDeleteSelf", "Cannot delete yourself");
}
