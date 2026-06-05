using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Auth.Commands.Logout;
using AppBaseNetReact.Application.Features.Auth.Notifications;

namespace AppBaseNetReact.Application.Features.Auth.Commands.Logout;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Unit>
{
    private readonly IUnitOfWork _uow;
    private readonly IJwtService _jwt;
    private readonly IMediator _mediator;

    public LogoutCommandHandler(
        IUnitOfWork uow,
        IJwtService jwt,
        IMediator mediator)
    {
        _uow = uow;
        _jwt = jwt;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(LogoutCommand request, CancellationToken ct)
    {
        var ip = request.IpAddress ?? "unknown";
        var ua = request.UserAgent ?? "unknown";
        var tokenHash = _jwt.HashRefreshToken(request.RefreshToken);
        var storedToken = await _uow.RefreshTokens.GetByTokenHashAsync(tokenHash, ct);

        if (storedToken != null)
        {
            storedToken.Revoke();
            await _uow.SaveChangesAsync(ct);
            await _mediator.Publish(new UserLoggedOutNotification(storedToken.UserId, ip, ua), ct);
        }

        return Unit.Value;
    }
}
