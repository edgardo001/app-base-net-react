using MediatR;

namespace AppBaseNetReact.Application.Features.Auth.Commands.Login;

public sealed record LoginCommand(
    string Email,
    string Password,
    string? IpAddress,
    string? UserAgent,
    string? FrontendUrl) : IRequest<LoginOutcome>;
