using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Roles.Notifications;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Features.Roles.Commands.UpdatePermissions;

public sealed class UpdatePermissionsCommandHandler : IRequestHandler<UpdatePermissionsCommand, UpdatePermissionsOutcome>
{
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public UpdatePermissionsCommandHandler(IUnitOfWork uow, IMediator mediator)
    {
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<UpdatePermissionsOutcome> Handle(UpdatePermissionsCommand request, CancellationToken ct)
    {
        var role = await _uow.Roles.GetByIdWithPermissionsAsync(request.RoleId, ct);
        if (role == null)
            return new UpdatePermissionsOutcome(UpdatePermissionsResult.NotFound());

        role.RolePermissions.Clear();

        foreach (var p in request.Permissions)
        {
            role.RolePermissions.Add(RolePermission.Create(request.RoleId, p.PermissionId, p.Granted));
        }

        await _uow.SaveChangesAsync(ct);

        await _mediator.Publish(new RolePermissionsUpdatedNotification(
            role.Id, role.Name, request.IpAddress, request.UserAgent), ct);

        return new UpdatePermissionsOutcome(UpdatePermissionsResult.Success());
    }
}
