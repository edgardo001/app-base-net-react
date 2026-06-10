namespace AppBaseNetReact.Application.Features.Users.Commands.CreateUser;

public sealed record CreateUserOutcome(CreateUserResult Result);

public sealed record CreateUserResult(bool IsSuccess, Guid? UserId = null, string? Email = null, string? ErrorCode = null, string? ErrorMessage = null)
{
    public static CreateUserResult Success(Guid userId, string email) => new(true, userId, email);
    public static CreateUserResult DuplicateEmail() => new(false, null, null, "DuplicateEmail", "Email already registered");
}
