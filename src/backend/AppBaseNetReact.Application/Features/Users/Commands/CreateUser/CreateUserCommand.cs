using MediatR;

namespace AppBaseNetReact.Application.Features.Users.Commands.CreateUser;

public sealed record CreateUserCommand(
    string Email,
    string FirstName,
    string LastName,
    List<Guid>? RoleIds,
    string FrontendBaseUrl,
    string? IpAddress,
    string? UserAgent) : IRequest<CreateUserOutcome>;
