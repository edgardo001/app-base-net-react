using MediatR;

namespace AppBaseNetReact.Application.Features.Auth.Commands.Refresh;

public sealed record RefreshCommand(
    string RefreshToken,
    string? IpAddress,
    string? UserAgent) : IRequest<RefreshOutcome>;
