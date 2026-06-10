using MediatR;

namespace AppBaseNetReact.Application.Features.Users.Commands.UpdateUser;

public sealed record UpdateUserCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    List<Guid>? RoleIds,
    string? IpAddress,
    string? UserAgent) : IRequest<UpdateUserOutcome>;
