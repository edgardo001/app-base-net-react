using FluentValidation;

namespace AppBaseNetReact.Application.Features.Users.Commands.ResendOnboardingEmail;

public sealed class ResendOnboardingEmailCommandValidator : AbstractValidator<ResendOnboardingEmailCommand>
{
    public ResendOnboardingEmailCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
