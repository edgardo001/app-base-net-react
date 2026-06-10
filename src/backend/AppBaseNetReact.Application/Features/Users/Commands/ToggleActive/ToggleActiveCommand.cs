using MediatR;

namespace AppBaseNetReact.Application.Features.Users.Commands.ToggleActive;

public sealed record ToggleActiveCommand(
    Guid UserId,
    bool Active,
    string? IpAddress,
    string? UserAgent) : IRequest<ToggleActiveOutcome>;
