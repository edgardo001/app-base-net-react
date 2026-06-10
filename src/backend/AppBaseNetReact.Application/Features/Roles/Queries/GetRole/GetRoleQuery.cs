using MediatR;

namespace AppBaseNetReact.Application.Features.Roles.Queries.GetRole;

public sealed record GetRoleQuery(Guid RoleId) : IRequest<GetRoleResponse?>;
