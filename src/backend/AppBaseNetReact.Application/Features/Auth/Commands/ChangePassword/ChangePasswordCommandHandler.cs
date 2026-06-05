using System.Security.Cryptography;
using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Common.Models;
using AppBaseNetReact.Application.Features.Auth.Commands.ChangePassword;
using AppBaseNetReact.Application.Features.Auth.Notifications;

namespace AppBaseNetReact.Application.Features.Auth.Commands.ChangePassword;

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, ChangePasswordOutcome>
{
    private const string InvalidCurrentPasswordMessage = "Current password is incorrect";

    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasherService _hasher;
    private readonly IPasswordPolicyService _passwordPolicy;
    private readonly IMediator _mediator;

    public ChangePasswordCommandHandler(
        IUnitOfWork uow,
        IPasswordHasherService hasher,
        IPasswordPolicyService passwordPolicy,
        IMediator mediator)
    {
        _uow = uow;
        _hasher = hasher;
        _passwordPolicy = passwordPolicy;
        _mediator = mediator;
    }

    public async Task<ChangePasswordOutcome> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(request.UserId, ct);
        if (user == null)
            return new ChangePasswordOutcome(
                PasswordResult.Fail(PasswordErrorCode.UserNotFound, "User not found"));

        if (!_hasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            return new ChangePasswordOutcome(
                PasswordResult.Fail(PasswordErrorCode.InvalidCurrentPassword, InvalidCurrentPasswordMessage));

        var (valid, error) = _passwordPolicy.Validate(request.NewPassword);
        if (!valid)
            return new ChangePasswordOutcome(
                PasswordResult.Fail(PasswordErrorCode.WeakPassword, error));

        user.SetPasswordHash(_hasher.HashPassword(request.NewPassword));
        await _uow.RefreshTokens.RevokeAllForUserAsync(user.Id, user.Id, ct);
        await _uow.SaveChangesAsync(ct);

        await _mediator.Publish(
            new PasswordChangedNotification(
                user.Id, user.Email, user.FirstName,
                request.IpAddress ?? "unknown", request.UserAgent ?? "unknown"),
            ct);

        return new ChangePasswordOutcome(PasswordResult.Success());
    }
}
