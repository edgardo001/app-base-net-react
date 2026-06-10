using MediatR;

namespace AppBaseNetReact.Application.Features.Permissions.Queries.GetModules;

public sealed record GetModulesQuery : IRequest<GetModulesResponse>;
