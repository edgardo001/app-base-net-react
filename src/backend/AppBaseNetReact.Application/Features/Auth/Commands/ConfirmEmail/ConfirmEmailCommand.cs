using MediatR;

namespace AppBaseNetReact.Application.Features.Auth.Commands.ConfirmEmail;

public sealed record ConfirmEmailCommand(
    string Token,
    string LoginLink,
    string? IpAddress,
    string? UserAgent) : IRequest<ConfirmEmailOutcome>;
