using MediatR;

namespace AppBaseNetReact.Application.Features.Roles.Queries.GetRoles;

public sealed record GetRolesQuery : IRequest<GetRolesResponse>;
