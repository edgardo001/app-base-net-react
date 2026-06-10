using MediatR;

namespace AppBaseNetReact.Application.Features.Profile.Commands.UpdateProfile;

public sealed record UpdateProfileCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    string IpAddress,
    string UserAgent) : IRequest<UpdateProfileOutcome>;
