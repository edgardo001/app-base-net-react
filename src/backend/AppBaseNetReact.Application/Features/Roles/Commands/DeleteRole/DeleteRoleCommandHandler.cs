using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Roles.Notifications;

namespace AppBaseNetReact.Application.Features.Roles.Commands.DeleteRole;

public sealed class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, DeleteRoleOutcome>
{
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public DeleteRoleCommandHandler(IUnitOfWork uow, IMediator mediator)
    {
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<DeleteRoleOutcome> Handle(DeleteRoleCommand request, CancellationToken ct)
    {
        var role = await _uow.Roles.GetByIdAsync(request.RoleId, ct);
        if (role == null)
            return new DeleteRoleOutcome(DeleteRoleResult.NotFound());

        if (role.IsSystem)
            return new DeleteRoleOutcome(DeleteRoleResult.CannotDeleteSystemRole());

        var roleName = role.Name;
        await _uow.Roles.DeleteAsync(role, ct);
        await _uow.SaveChangesAsync(ct);

        await _mediator.Publish(new RoleDeletedNotification(
            role.Id, roleName, request.DeletedBy, request.IpAddress, request.UserAgent), ct);

        return new DeleteRoleOutcome(DeleteRoleResult.Success());
    }
}
