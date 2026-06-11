using MediatR;

namespace AppBaseNetReact.Application.Features.Auth.Commands.GoogleLogin;

public record GoogleLoginCommand(
    string Code,
    string State,
    string? IpAddress,
    string? UserAgent,
    string FrontendUrl) : IRequest<GoogleLoginOutcome>;
