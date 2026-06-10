using FluentValidation;

namespace AppBaseNetReact.Application.Features.Users.Commands.RevokeTokens;

public sealed class RevokeTokensCommandValidator : AbstractValidator<RevokeTokensCommand>
{
    public RevokeTokensCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
