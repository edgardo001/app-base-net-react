using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;

namespace AppBaseNetReact.Application.Features.Admin.Commands.SendTestEmail;

public sealed class SendTestEmailCommandHandler : IRequestHandler<SendTestEmailCommand, SendTestEmailOutcome>
{
    private readonly IEmailService _email;
    private readonly IAuditService _audit;

    public SendTestEmailCommandHandler(IEmailService email, IAuditService audit)
    {
        _email = email;
        _audit = audit;
    }

    public async Task<SendTestEmailOutcome> Handle(SendTestEmailCommand request, CancellationToken ct)
    {
        try
        {
            await _email.SendEmailAsync(request.To, request.Subject, request.HtmlBody, ct);

            await _audit.LogAsync(
                "TestEmailSent", "Email", null,
                $"Test email sent to {request.To}", null, request.UserId,
                request.IpAddress, request.UserAgent, null, ct);

            return SendTestEmailOutcome.Success();
        }
        catch (Exception ex)
        {
            return SendTestEmailOutcome.Failed(ex.Message);
        }
    }
}
