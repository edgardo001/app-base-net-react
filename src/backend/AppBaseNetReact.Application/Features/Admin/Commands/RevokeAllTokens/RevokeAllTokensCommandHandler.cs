using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;

namespace AppBaseNetReact.Application.Features.Admin.Commands.RevokeAllTokens;

public sealed class RevokeAllTokensCommandHandler : IRequestHandler<RevokeAllTokensCommand, RevokeAllTokensOutcome>
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;

    public RevokeAllTokensCommandHandler(IUnitOfWork uow, IAuditService audit)
    {
        _uow = uow;
        _audit = audit;
    }

    public async Task<RevokeAllTokensOutcome> Handle(RevokeAllTokensCommand request, CancellationToken ct)
    {
        await _uow.RefreshTokens.RevokeAllGlobalAsync(request.UserId, ct);
        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "AllTokensRevoked", "System", null,
            null, null, request.UserId,
            request.IpAddress, request.UserAgent, null, ct);

        return RevokeAllTokensOutcome.Success();
    }
}
