using MediatR;
using AppBaseNetReact.Application.Common.Models;

namespace AppBaseNetReact.Application.Features.Users.Commands.ResendOnboardingEmail;

public sealed record ResendOnboardingEmailOutcome(ResendOnboardingEmailResult Result);
