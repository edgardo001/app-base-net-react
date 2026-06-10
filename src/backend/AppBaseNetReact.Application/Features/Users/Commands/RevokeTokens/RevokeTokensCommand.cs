using MediatR;

namespace AppBaseNetReact.Application.Features.Users.Commands.RevokeTokens;

public sealed record RevokeTokensCommand(
    Guid UserId,
    string? IpAddress,
    string? UserAgent) : IRequest<RevokeTokensOutcome>;
