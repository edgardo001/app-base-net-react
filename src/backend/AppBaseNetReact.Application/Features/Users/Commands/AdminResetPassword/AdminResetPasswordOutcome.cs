namespace AppBaseNetReact.Application.Features.Users.Commands.AdminResetPassword;

public sealed record AdminResetPasswordOutcome(AdminResetPasswordResult Result);

public sealed record AdminResetPasswordResult(bool IsSuccess, string? TemporaryPassword = null, string? ErrorCode = null, string? ErrorMessage = null)
{
    public static AdminResetPasswordResult Success(string temporaryPassword) => new(true, temporaryPassword);
    public static AdminResetPasswordResult UserNotFound() => new(false, null, "UserNotFound", "User not found");
}
