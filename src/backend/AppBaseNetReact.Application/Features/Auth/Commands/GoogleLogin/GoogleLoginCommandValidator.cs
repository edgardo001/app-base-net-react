using FluentValidation;

namespace AppBaseNetReact.Application.Features.Auth.Commands.GoogleLogin;

public sealed class GoogleLoginCommandValidator : AbstractValidator<GoogleLoginCommand>
{
    public GoogleLoginCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Authorization code is required");
        RuleFor(x => x.State).NotEmpty().WithMessage("State parameter is required");
    }
}
