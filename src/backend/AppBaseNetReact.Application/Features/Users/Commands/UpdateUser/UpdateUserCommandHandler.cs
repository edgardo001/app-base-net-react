using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Users.Notifications;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Features.Users.Commands.UpdateUser;

public sealed class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UpdateUserOutcome>
{
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public UpdateUserCommandHandler(IUnitOfWork uow, IMediator mediator)
    {
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<UpdateUserOutcome> Handle(UpdateUserCommand request, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdWithRolesAsync(request.UserId, ct);
        if (user == null)
            return new UpdateUserOutcome(UpdateUserResult.UserNotFound());

        user.UpdateProfile(request.FirstName, request.LastName);

        if (request.RoleIds != null)
        {
            user.UserRoles.Clear();
            foreach (var roleId in request.RoleIds)
                user.UserRoles.Add(UserRole.Create(user.Id, roleId));
        }

        await _uow.SaveChangesAsync(ct);

        await _mediator.Publish(new UserUpdatedNotification(
            user.Id, user.Email,
            request.IpAddress ?? "unknown", request.UserAgent ?? "unknown"), ct);

        return new UpdateUserOutcome(UpdateUserResult.Success());
    }
}
