using MediatR;

namespace AppBaseNetReact.Application.Features.Auth.Commands.Logout;

public sealed record LogoutCommand(
    string RefreshToken,
    string? IpAddress,
    string? UserAgent) : IRequest<Unit>;
