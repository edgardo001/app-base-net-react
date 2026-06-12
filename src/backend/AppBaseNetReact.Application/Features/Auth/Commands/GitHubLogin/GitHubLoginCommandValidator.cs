using FluentValidation;

namespace AppBaseNetReact.Application.Features.Auth.Commands.GitHubLogin;

public sealed class GitHubLoginCommandValidator : AbstractValidator<GitHubLoginCommand>
{
    public GitHubLoginCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Authorization code is required");
        RuleFor(x => x.State).NotEmpty().WithMessage("State parameter is required");
    }
}
