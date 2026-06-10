using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Roles.Notifications;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Features.Roles.Commands.CreateRole;

public sealed class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, CreateRoleOutcome>
{
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public CreateRoleCommandHandler(IUnitOfWork uow, IMediator mediator)
    {
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<CreateRoleOutcome> Handle(CreateRoleCommand request, CancellationToken ct)
    {
        var existing = await _uow.Roles.GetByNameAsync(request.Name, ct);
        if (existing != null)
            return new CreateRoleOutcome(CreateRoleResult.DuplicateName());

        var role = Role.Create(request.Name, request.Description);
        await _uow.Roles.AddAsync(role, ct);
        await _uow.SaveChangesAsync(ct);

        await _mediator.Publish(new RoleCreatedNotification(
            role.Id, role.Name, request.IpAddress, request.UserAgent), ct);

        return new CreateRoleOutcome(CreateRoleResult.Success(role.Id, role.Name));
    }
}
