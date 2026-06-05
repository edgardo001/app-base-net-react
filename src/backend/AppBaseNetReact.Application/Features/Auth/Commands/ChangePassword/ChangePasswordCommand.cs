using MediatR;

namespace AppBaseNetReact.Application.Features.Auth.Commands.ChangePassword;

public sealed record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword,
    string? IpAddress,
    string? UserAgent) : IRequest<ChangePasswordOutcome>;
