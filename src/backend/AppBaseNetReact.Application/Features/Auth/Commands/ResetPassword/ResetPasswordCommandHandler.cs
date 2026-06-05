using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Common.Models;
using AppBaseNetReact.Application.Features.Auth.Commands.ResetPassword;
using AppBaseNetReact.Application.Features.Auth.Notifications;

namespace AppBaseNetReact.Application.Features.Auth.Commands.ResetPassword;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordOutcome>
{
    private const string InvalidResetTokenMessage = "Invalid reset token";
    private const string ResetTokenExpiredMessage = "Reset token has expired";

    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasherService _hasher;
    private readonly IPasswordPolicyService _passwordPolicy;
    private readonly IDateTimeProvider _clock;
    private readonly IMediator _mediator;

    public ResetPasswordCommandHandler(
        IUnitOfWork uow,
        IPasswordHasherService hasher,
        IPasswordPolicyService passwordPolicy,
        IDateTimeProvider clock,
        IMediator mediator)
    {
        _uow = uow;
        _hasher = hasher;
        _passwordPolicy = passwordPolicy;
        _clock = clock;
        _mediator = mediator;
    }

    public async Task<ResetPasswordOutcome> Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var user = await _uow.Users.GetByEmailConfirmationTokenAsync(request.Token, ct);
        if (user == null)
            return new ResetPasswordOutcome(
                PasswordResult.Fail(PasswordErrorCode.InvalidResetToken, InvalidResetTokenMessage));

        if (user.EmailConfirmationTokenExpires < _clock.UtcNow)
            return new ResetPasswordOutcome(
                PasswordResult.Fail(PasswordErrorCode.ResetTokenExpired, ResetTokenExpiredMessage));

        var (valid, error) = _passwordPolicy.Validate(request.NewPassword);
        if (!valid)
            return new ResetPasswordOutcome(
                PasswordResult.Fail(PasswordErrorCode.WeakPassword, error));

        user.SetPasswordHash(_hasher.HashPassword(request.NewPassword));
        user.ForcePasswordChange();
        user.ConfirmEmail();
        await _uow.SaveChangesAsync(ct);

        await _mediator.Publish(
            new PasswordResetNotification(
                user.Id, user.Email, user.FirstName,
                request.IpAddress ?? "unknown", request.UserAgent ?? "unknown"),
            ct);

        return new ResetPasswordOutcome(PasswordResult.Success());
    }
}
