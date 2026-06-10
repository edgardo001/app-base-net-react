using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;

namespace AppBaseNetReact.Application.Features.Roles.Queries.GetUsersByRole;

public sealed class GetUsersByRoleQueryHandler : IRequestHandler<GetUsersByRoleQuery, GetUsersByRoleResponse?>
{
    private readonly IUnitOfWork _uow;

    public GetUsersByRoleQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<GetUsersByRoleResponse?> Handle(GetUsersByRoleQuery request, CancellationToken ct)
    {
        var role = await _uow.Roles.GetByIdAsync(request.RoleId, ct);
        if (role == null) return null;

        var users = await _uow.Users.GetUsersByRoleAsync(request.RoleId, ct);

        return new GetUsersByRoleResponse
        {
            Users = users.Select(u => new UserByRoleDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsActive = u.IsActive,
                LastLoginAt = u.LastLoginAt
            }).ToList()
        };
    }
}
