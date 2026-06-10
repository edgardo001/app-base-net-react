using MediatR;

namespace AppBaseNetReact.Application.Features.Users.Queries.GetUsers;

public sealed record GetUsersQuery(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    string? SortBy = null,
    bool SortDesc = false) : IRequest<GetUsersResponse>;
