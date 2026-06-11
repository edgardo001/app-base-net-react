using System.Security.Cryptography;
using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Common.Models;
using AppBaseNetReact.Application.Features.Auth.Notifications;
using AppBaseNetReact.Domain.Entities;

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

        var historyValid = await _passwordPolicy.CheckPasswordHistoryAsync(user.Id, request.NewPassword, ct);
        if (!historyValid)
            return new ChangePasswordOutcome(
                PasswordResult.Fail(PasswordErrorCode.WeakPassword, "Password has been used recently. Choose a different password."));

        var newHash = _hasher.HashPassword(request.NewPassword);
        user.SetPasswordHash(newHash);
        await _uow.RefreshTokens.RevokeAllForUserAsync(user.Id, user.Id, ct);

        var historyEntry = PasswordHistory.Create(user.Id, newHash);
        await _uow.PasswordHistories.AddAsync(historyEntry, ct);

        var historyCount = await _uow.PasswordHistories.CountAsync(ph => ph.UserId == user.Id, ct);
        if (historyCount > _passwordPolicy.PasswordHistoryCount)
            await _uow.PasswordHistories.DeleteOldestForUserAsync(user.Id, ct);

        await _uow.SaveChangesAsync(ct);

        await _mediator.Publish(
            new PasswordChangedNotification(
                user.Id, user.Email, user.FirstName,
                request.IpAddress ?? "unknown", request.UserAgent ?? "unknown"),
            ct);

        return new ChangePasswordOutcome(PasswordResult.Success());
    }
}
