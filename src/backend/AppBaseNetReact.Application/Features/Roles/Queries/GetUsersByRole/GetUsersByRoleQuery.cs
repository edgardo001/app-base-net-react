using MediatR;

namespace AppBaseNetReact.Application.Features.Roles.Queries.GetUsersByRole;

public sealed record GetUsersByRoleQuery(Guid RoleId) : IRequest<GetUsersByRoleResponse?>;
