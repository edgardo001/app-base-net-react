using System.Security.Cryptography;
using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Common.Models;
using AppBaseNetReact.Application.Features.Auth.Notifications;
using AppBaseNetReact.Application.Features.Users.Commands.ResendOnboardingEmail;

namespace AppBaseNetReact.Application.Features.Users.Commands.ResendOnboardingEmail;

public sealed class ResendOnboardingEmailCommandHandler : IRequestHandler<ResendOnboardingEmailCommand, ResendOnboardingEmailOutcome>
{
    private const string UserNotFoundMessage = "User not found";
    private const string AlreadyConfirmedMessage = "User has already confirmed their email";

    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;
    private readonly IMediator _mediator;

    public ResendOnboardingEmailCommandHandler(
        IUnitOfWork uow,
        IDateTimeProvider clock,
        IMediator mediator)
    {
        _uow = uow;
        _clock = clock;
        _mediator = mediator;
    }

    public async Task<ResendOnboardingEmailOutcome> Handle(ResendOnboardingEmailCommand request, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(request.UserId, ct);
        if (user == null)
            return new ResendOnboardingEmailOutcome(
                ResendOnboardingEmailResult.Fail(ResendOnboardingErrorCode.UserNotFound, UserNotFoundMessage));

        if (user.EmailConfirmed)
            return new ResendOnboardingEmailOutcome(
                ResendOnboardingEmailResult.Fail(ResendOnboardingErrorCode.AlreadyConfirmed, AlreadyConfirmedMessage));

        var newToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        user.SetEmailConfirmationToken(newToken, _clock.UtcNow.AddHours(24));
        await _uow.SaveChangesAsync(ct);

        await _mediator.Publish(
            new OnboardingEmailResentNotification(
                user.Id, user.Email, user.FirstName, newToken,
                request.IpAddress ?? "unknown", request.UserAgent ?? "unknown"),
            ct);

        return new ResendOnboardingEmailOutcome(ResendOnboardingEmailResult.Success());
    }
}
