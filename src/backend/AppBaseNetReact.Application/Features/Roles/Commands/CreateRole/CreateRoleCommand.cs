using MediatR;

namespace AppBaseNetReact.Application.Features.Roles.Commands.CreateRole;

public sealed record CreateRoleCommand(
    string Name,
    string Description,
    string IpAddress,
    string UserAgent) : IRequest<CreateRoleOutcome>;
