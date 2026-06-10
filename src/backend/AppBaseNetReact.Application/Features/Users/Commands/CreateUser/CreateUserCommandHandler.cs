using System.Security.Cryptography;
using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Users.Notifications;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Features.Users.Commands.CreateUser;

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, CreateUserOutcome>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasherService _hasher;
    private readonly IRandomPasswordGenerator _passwords;
    private readonly IMediator _mediator;

    public CreateUserCommandHandler(
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

    public async Task<CreateUserOutcome> Handle(CreateUserCommand request, CancellationToken ct)
    {
        var existing = await _uow.Users.GetByEmailAsync(request.Email, ct);
        if (existing != null)
            return new CreateUserOutcome(CreateUserResult.DuplicateEmail());

        var temporaryPassword = _passwords.Generate();
        var user = User.Create(
            request.Email,
            request.FirstName,
            request.LastName,
            _hasher.HashPassword(temporaryPassword));
        user.ForcePasswordChange();

        var confirmationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        user.SetEmailConfirmationToken(confirmationToken, DateTime.UtcNow.AddHours(24));

        if (request.RoleIds?.Any() == true)
        {
            foreach (var roleId in request.RoleIds)
                user.UserRoles.Add(UserRole.Create(user.Id, roleId));
        }

        await _uow.Users.AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);

        await _mediator.Publish(new UserCreatedNotification(
            user.Id, user.Email, user.FirstName,
            confirmationToken, temporaryPassword, request.FrontendBaseUrl,
            request.IpAddress ?? "unknown", request.UserAgent ?? "unknown"), ct);

        return new CreateUserOutcome(CreateUserResult.Success(user.Id, user.Email));
    }
}
