using MediatR;

namespace AppBaseNetReact.Application.Features.Admin.Commands.SendTestEmail;

public sealed record SendTestEmailCommand(
    string To,
    string Subject,
    string HtmlBody,
    Guid? UserId,
    string IpAddress,
    string UserAgent) : IRequest<SendTestEmailOutcome>;
