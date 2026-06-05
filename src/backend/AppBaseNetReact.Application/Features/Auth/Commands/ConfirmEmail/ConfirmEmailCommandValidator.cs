using FluentValidation;

namespace AppBaseNetReact.Application.Features.Auth.Commands.ConfirmEmail;

public sealed class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.LoginLink).NotEmpty();
    }
}
