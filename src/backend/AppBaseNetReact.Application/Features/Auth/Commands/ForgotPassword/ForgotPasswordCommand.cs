using MediatR;

namespace AppBaseNetReact.Application.Features.Auth.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(
    string Email,
    string? IpAddress,
    string? UserAgent) : IRequest<ForgotPasswordOutcome>;
