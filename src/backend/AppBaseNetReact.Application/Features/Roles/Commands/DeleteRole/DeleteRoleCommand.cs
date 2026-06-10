using MediatR;

namespace AppBaseNetReact.Application.Features.Roles.Commands.DeleteRole;

public sealed record DeleteRoleCommand(
    Guid RoleId,
    Guid? DeletedBy,
    string IpAddress,
    string UserAgent) : IRequest<DeleteRoleOutcome>;
