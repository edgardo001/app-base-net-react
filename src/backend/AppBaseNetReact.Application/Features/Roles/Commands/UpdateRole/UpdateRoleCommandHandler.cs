using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Roles.Notifications;

namespace AppBaseNetReact.Application.Features.Roles.Commands.UpdateRole;

public sealed class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, UpdateRoleOutcome>
{
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public UpdateRoleCommandHandler(IUnitOfWork uow, IMediator mediator)
    {
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<UpdateRoleOutcome> Handle(UpdateRoleCommand request, CancellationToken ct)
    {
        var role = await _uow.Roles.GetByIdAsync(request.RoleId, ct);
        if (role == null)
            return new UpdateRoleOutcome(UpdateRoleResult.NotFound());

        if (role.IsSystem)
            return new UpdateRoleOutcome(UpdateRoleResult.CannotModifySystemRole());

        var oldName = role.Name;
        role.Update(request.Name, request.Description);
        await _uow.SaveChangesAsync(ct);

        await _mediator.Publish(new RoleUpdatedNotification(
            role.Id, oldName, role.Name, request.IpAddress, request.UserAgent), ct);

        return new UpdateRoleOutcome(UpdateRoleResult.Success());
    }
}
