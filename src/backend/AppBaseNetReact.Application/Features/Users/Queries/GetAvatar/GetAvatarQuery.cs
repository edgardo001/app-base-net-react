using MediatR;

namespace AppBaseNetReact.Application.Features.Users.Queries.GetAvatar;

public sealed record GetAvatarQuery(Guid UserId) : IRequest<GetAvatarOutcome>;
