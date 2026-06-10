using FluentValidation;

namespace AppBaseNetReact.Application.Features.Users.Commands.ToggleActive;

public sealed class ToggleActiveCommandValidator : AbstractValidator<ToggleActiveCommand>
{
    public ToggleActiveCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
