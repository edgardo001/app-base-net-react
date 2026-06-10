using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;

namespace AppBaseNetReact.Application.Features.Roles.Queries.GetRole;

public sealed class GetRoleQueryHandler : IRequestHandler<GetRoleQuery, GetRoleResponse?>
{
    private readonly IUnitOfWork _uow;

    public GetRoleQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<GetRoleResponse?> Handle(GetRoleQuery request, CancellationToken ct)
    {
        var role = await _uow.Roles.GetByIdWithPermissionsAsync(request.RoleId, ct);
        if (role == null) return null;

        return new GetRoleResponse
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsSystem = role.IsSystem,
            CreatedAt = role.CreatedAt,
            Permissions = role.RolePermissions.Select(rp => new RolePermissionDto
            {
                Id = rp.Permission.Id,
                Code = rp.Permission.Code,
                Name = rp.Permission.Name,
                Module = rp.Permission.Module,
                Granted = rp.Granted
            }).ToList()
        };
    }
}
