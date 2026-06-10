using MediatR;

namespace AppBaseNetReact.Application.Features.Users.Commands.AdminResetPassword;

public sealed record AdminResetPasswordCommand(
    Guid UserId,
    string LoginLink,
    string? IpAddress,
    string? UserAgent) : IRequest<AdminResetPasswordOutcome>;
