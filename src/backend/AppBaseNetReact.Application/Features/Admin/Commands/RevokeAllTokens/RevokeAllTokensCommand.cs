using MediatR;

namespace AppBaseNetReact.Application.Features.Admin.Commands.RevokeAllTokens;

public sealed record RevokeAllTokensCommand(
    Guid? UserId,
    string IpAddress,
    string UserAgent) : IRequest<RevokeAllTokensOutcome>;
