using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Users.Notifications;

namespace AppBaseNetReact.Application.Features.Users.Commands.DeleteUser;

public sealed class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, DeleteUserOutcome>
{
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public DeleteUserCommandHandler(IUnitOfWork uow, IMediator mediator)
    {
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<DeleteUserOutcome> Handle(DeleteUserCommand request, CancellationToken ct)
    {
        if (request.CurrentUserId == request.UserId)
            return new DeleteUserOutcome(DeleteUserResult.CannotDeleteSelf());

        var user = await _uow.Users.GetByIdAsync(request.UserId, ct);
        if (user == null)
            return new DeleteUserOutcome(DeleteUserResult.UserNotFound());

        user.SoftDelete(request.CurrentUserId);
        await _uow.SaveChangesAsync(ct);

        await _mediator.Publish(new UserDeletedNotification(
            user.Id, user.Email, request.CurrentUserId,
            request.IpAddress ?? "unknown", request.UserAgent ?? "unknown"), ct);

        return new DeleteUserOutcome(DeleteUserResult.Success());
    }
}
