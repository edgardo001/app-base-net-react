using FluentValidation;

namespace AppBaseNetReact.Application.Features.Users.Commands.AdminResetPassword;

public sealed class AdminResetPasswordCommandValidator : AbstractValidator<AdminResetPasswordCommand>
{
    public AdminResetPasswordCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
