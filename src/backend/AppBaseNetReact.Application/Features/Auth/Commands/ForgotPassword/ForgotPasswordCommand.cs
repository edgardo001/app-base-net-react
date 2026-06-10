using MediatR;

namespace AppBaseNetReact.Application.Features.Auth.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(
    string Email,
    string? IpAddress,
    string? UserAgent,
    string? FrontendUrl,
    string? CaptchaToken) : IRequest<ForgotPasswordOutcome>;
