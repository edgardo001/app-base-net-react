using MediatR;

namespace AppBaseNetReact.Application.Features.Users.Queries.GetUser;

public sealed record GetUserQuery(Guid UserId) : IRequest<GetUserResponse?>;
