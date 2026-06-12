using MediatR;

namespace AppBaseNetReact.Application.Features.Auth.Commands.GitHubLogin;

public record GitHubLoginCommand(
    string Code,
    string State,
    string? IpAddress,
    string? UserAgent,
    string FrontendUrl) : IRequest<GitHubLoginOutcome>;
