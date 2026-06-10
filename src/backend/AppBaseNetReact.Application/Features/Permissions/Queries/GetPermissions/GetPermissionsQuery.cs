using MediatR;

namespace AppBaseNetReact.Application.Features.Permissions.Queries.GetPermissions;

public sealed record GetPermissionsQuery : IRequest<GetPermissionsResponse>;
