using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Users.Notifications;

namespace AppBaseNetReact.Application.Features.Users.Commands.ToggleActive;

public sealed class ToggleActiveCommandHandler : IRequestHandler<ToggleActiveCommand, ToggleActiveOutcome>
{
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public ToggleActiveCommandHandler(IUnitOfWork uow, IMediator mediator)
    {
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ToggleActiveOutcome> Handle(ToggleActiveCommand request, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(request.UserId, ct);
        if (user == null)
            return new ToggleActiveOutcome(ToggleActiveResult.UserNotFound());

        user.SetActive(request.Active);
        await _uow.SaveChangesAsync(ct);

        if (request.Active)
            await _mediator.Publish(new UserActivatedNotification(
                user.Id, user.Email, request.Active,
                request.IpAddress ?? "unknown", request.UserAgent ?? "unknown"), ct);
        else
            await _mediator.Publish(new UserDeactivatedNotification(
                user.Id, user.Email, request.Active,
                request.IpAddress ?? "unknown", request.UserAgent ?? "unknown"), ct);

        return new ToggleActiveOutcome(ToggleActiveResult.Success(request.Active));
    }
}
