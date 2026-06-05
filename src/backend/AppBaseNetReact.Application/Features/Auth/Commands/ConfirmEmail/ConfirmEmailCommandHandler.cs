using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Common.Models;
using AppBaseNetReact.Application.Features.Auth.Notifications;

namespace AppBaseNetReact.Application.Features.Auth.Commands.ConfirmEmail;

public sealed class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, ConfirmEmailOutcome>
{
    private const string InvalidTokenMessage = "Invalid confirmation token";
    private const string ExpiredTokenMessage = "Confirmation token has expired";

    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;
    private readonly IMediator _mediator;

    public ConfirmEmailCommandHandler(
        IUnitOfWork uow,
        IDateTimeProvider clock,
        IMediator mediator)
    {
        _uow = uow;
        _clock = clock;
        _mediator = mediator;
    }

    public async Task<ConfirmEmailOutcome> Handle(ConfirmEmailCommand request, CancellationToken ct)
    {
        var user = await _uow.Users.GetByEmailConfirmationTokenAsync(request.Token, ct);
        if (user == null)
            return new ConfirmEmailOutcome(
                EmailConfirmationResult.Fail(EmailErrorCode.InvalidConfirmationToken, InvalidTokenMessage));

        if (user.EmailConfirmationTokenExpires < _clock.UtcNow)
            return new ConfirmEmailOutcome(
                EmailConfirmationResult.Fail(EmailErrorCode.ConfirmationTokenExpired, ExpiredTokenMessage));

        user.ConfirmEmail();
        await _uow.SaveChangesAsync(ct);

        await _mediator.Publish(
            new EmailConfirmedNotification(
                user.Id, user.Email, user.FirstName,
                request.IpAddress ?? "unknown", request.UserAgent ?? "unknown",
                request.LoginLink),
            ct);

        return new ConfirmEmailOutcome(EmailConfirmationResult.Success());
    }
}
