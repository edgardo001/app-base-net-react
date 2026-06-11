using MediatR;

namespace AppBaseNetReact.Application.Features.Users.Commands.ImportUsers;

public sealed record ImportUsersCommand(
    Stream FileContent,
    string FileName,
    string? IpAddress,
    string? UserAgent) : IRequest<ImportUsersResult>;

public sealed record ImportUsersRow(string Email, string FirstName, string LastName, List<Guid>? RoleIds = null);
