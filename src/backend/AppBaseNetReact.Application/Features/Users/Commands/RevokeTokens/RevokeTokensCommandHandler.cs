using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Users.Notifications;

namespace AppBaseNetReact.Application.Features.Users.Commands.RevokeTokens;

public sealed class RevokeTokensCommandHandler : IRequestHandler<RevokeTokensCommand, RevokeTokensOutcome>
{
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public RevokeTokensCommandHandler(IUnitOfWork uow, IMediator mediator)
    {
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<RevokeTokensOutcome> Handle(RevokeTokensCommand request, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(request.UserId, ct);
        if (user == null)
            return new RevokeTokensOutcome(RevokeTokensResult.UserNotFound());

        await _uow.RefreshTokens.RevokeAllForUserAsync(request.UserId, null, ct);
        await _uow.SaveChangesAsync(ct);

        await _mediator.Publish(new TokensRevokedNotification(
            request.UserId,
            request.IpAddress ?? "unknown",
            request.UserAgent ?? "unknown"), ct);

        return new RevokeTokensOutcome(RevokeTokensResult.Success());
    }
}
