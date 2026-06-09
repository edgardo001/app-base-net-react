using System.Security.Cryptography;
using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Common.Models;
using AppBaseNetReact.Application.Features.Auth.Commands.ForgotPassword;
using AppBaseNetReact.Application.Features.Auth.Notifications;

namespace AppBaseNetReact.Application.Features.Auth.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordOutcome>
{
    private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromHours(24);

    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;
    private readonly IMediator _mediator;

    public ForgotPasswordCommandHandler(
        IUnitOfWork uow,
        IDateTimeProvider clock,
        IMediator mediator)
    {
        _uow = uow;
        _clock = clock;
        _mediator = mediator;
    }

    public async Task<ForgotPasswordOutcome> Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var user = await _uow.Users.GetByEmailAsync(request.Email, ct);
        if (user == null)
            return new ForgotPasswordOutcome(PasswordResult.Success());

        var resetToken = GenerateToken();
        user.SetEmailConfirmationToken(resetToken, _clock.UtcNow.Add(ResetTokenLifetime));
        await _uow.SaveChangesAsync(ct);

        var frontendUrl = (request.FrontendUrl ?? "http://localhost:5173").TrimEnd('/');
        var resetLink = $"{frontendUrl}/reset-password?token={resetToken}";

        await _mediator.Publish(
            new PasswordResetRequestedNotification(
                user.Id, user.Email, user.FirstName, resetLink,
                request.IpAddress ?? "unknown", request.UserAgent ?? "unknown"),
            ct);

        return new ForgotPasswordOutcome(PasswordResult.Success());
    }

    private static string GenerateToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    }
}
