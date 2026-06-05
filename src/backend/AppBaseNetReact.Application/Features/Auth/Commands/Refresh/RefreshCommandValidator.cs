using FluentValidation;

namespace AppBaseNetReact.Application.Features.Auth.Commands.Refresh;

public sealed class RefreshCommandValidator : AbstractValidator<RefreshCommand>
{
    public RefreshCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
