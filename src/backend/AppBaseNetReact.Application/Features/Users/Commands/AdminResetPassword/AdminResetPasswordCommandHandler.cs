using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Users.Notifications;

namespace AppBaseNetReact.Application.Features.Users.Commands.AdminResetPassword;

public sealed class AdminResetPasswordCommandHandler : IRequestHandler<AdminResetPasswordCommand, AdminResetPasswordOutcome>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasherService _hasher;
    private readonly IRandomPasswordGenerator _passwords;
    private readonly IMediator _mediator;

    public AdminResetPasswordCommandHandler(
        IUnitOfWork uow,
        IPasswordHasherService hasher,
        IRandomPasswordGenerator passwords,
        IMediator mediator)
    {
        _uow = uow;
        _hasher = hasher;
        _passwords = passwords;
        _mediator = mediator;
    }

    public async Task<AdminResetPasswordOutcome> Handle(AdminResetPasswordCommand request, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(request.UserId, ct);
        if (user == null)
            return new AdminResetPasswordOutcome(AdminResetPasswordResult.UserNotFound());

        var tempPassword = _passwords.Generate();
        user.SetPasswordHash(_hasher.HashPassword(tempPassword));
        user.ConfirmEmail();
        await _uow.SaveChangesAsync(ct);

        await _mediator.Publish(new PasswordResetByAdminNotification(
            user.Id, user.Email, user.FirstName,
            tempPassword, request.LoginLink,
            request.IpAddress ?? "unknown", request.UserAgent ?? "unknown"), ct);

        return new AdminResetPasswordOutcome(AdminResetPasswordResult.Success(tempPassword));
    }
}
