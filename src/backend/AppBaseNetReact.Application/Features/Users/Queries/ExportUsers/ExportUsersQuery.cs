using MediatR;

namespace AppBaseNetReact.Application.Features.Users.Queries.ExportUsers;

public sealed record ExportUsersQuery(
    string? Search = null,
    string? SortBy = null,
    bool SortDesc = false) : IRequest<byte[]>;
