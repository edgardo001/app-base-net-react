using MediatR;

namespace AppBaseNetReact.Application.Features.Profile.Queries.GetActivity;

public sealed record GetActivityQuery(Guid UserId) : IRequest<GetActivityResponse>;
