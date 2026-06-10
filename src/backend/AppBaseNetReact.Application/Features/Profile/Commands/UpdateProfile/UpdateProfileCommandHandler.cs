using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;

namespace AppBaseNetReact.Application.Features.Profile.Commands.UpdateProfile;

public sealed class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, UpdateProfileOutcome>
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;

    public UpdateProfileCommandHandler(IUnitOfWork uow, IAuditService audit)
    {
        _uow = uow;
        _audit = audit;
    }

    public async Task<UpdateProfileOutcome> Handle(UpdateProfileCommand request, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(request.UserId, ct);
        if (user == null)
            return UpdateProfileOutcome.UserNotFound();

        var oldFirstName = user.FirstName;
        var oldLastName = user.LastName;
        var oldValues = $"{{\"firstName\":\"{oldFirstName}\",\"lastName\":\"{oldLastName}\"}}";
        var newValues = $"{{\"firstName\":\"{request.FirstName}\",\"lastName\":\"{request.LastName}\"}}";

        user.UpdateProfile(request.FirstName, request.LastName);
        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "ProfileUpdated", "User", user.Id.ToString(),
            oldValues, newValues, request.UserId,
            request.IpAddress, request.UserAgent,
            null, ct);

        return UpdateProfileOutcome.Success();
    }
}
