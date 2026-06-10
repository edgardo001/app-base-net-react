using MediatR;

namespace AppBaseNetReact.Application.Features.Users.Commands.DeleteUser;

public sealed record DeleteUserCommand(
    Guid UserId,
    Guid? CurrentUserId,
    string? IpAddress,
    string? UserAgent) : IRequest<DeleteUserOutcome>;
