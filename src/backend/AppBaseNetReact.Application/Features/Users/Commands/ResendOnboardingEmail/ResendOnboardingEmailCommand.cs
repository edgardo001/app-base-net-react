using MediatR;
using AppBaseNetReact.Application.Common.Models;

namespace AppBaseNetReact.Application.Features.Users.Commands.ResendOnboardingEmail;

public sealed record ResendOnboardingEmailCommand(
    Guid UserId,
    string? IpAddress,
    string? UserAgent) : IRequest<ResendOnboardingEmailOutcome>;
