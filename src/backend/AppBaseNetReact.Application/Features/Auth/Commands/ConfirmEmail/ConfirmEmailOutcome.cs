using AppBaseNetReact.Application.Common.Models;

namespace AppBaseNetReact.Application.Features.Auth.Commands.ConfirmEmail;

public sealed record ConfirmEmailOutcome(EmailConfirmationResult Result);
