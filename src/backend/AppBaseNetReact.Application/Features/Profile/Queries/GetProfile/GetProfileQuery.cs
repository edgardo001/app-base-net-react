using MediatR;

namespace AppBaseNetReact.Application.Features.Profile.Queries.GetProfile;

public sealed record GetProfileQuery(Guid UserId) : IRequest<GetProfileResponse?>;
