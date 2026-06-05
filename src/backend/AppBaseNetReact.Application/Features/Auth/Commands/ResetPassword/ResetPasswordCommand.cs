using MediatR;

namespace AppBaseNetReact.Application.Features.Auth.Commands.ResetPassword;

public sealed record ResetPasswordCommand(
    string Token,
    string NewPassword,
    string? IpAddress,
    string? UserAgent) : IRequest<ResetPasswordOutcome>;
