using MediatR;

namespace AppBaseNetReact.Application.Features.Roles.Commands.UpdateRole;

public sealed record UpdateRoleCommand(
    Guid RoleId,
    string Name,
    string Description,
    string IpAddress,
    string UserAgent) : IRequest<UpdateRoleOutcome>;
