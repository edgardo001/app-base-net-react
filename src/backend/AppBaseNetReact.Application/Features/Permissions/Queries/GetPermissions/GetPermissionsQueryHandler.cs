using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;

namespace AppBaseNetReact.Application.Features.Permissions.Queries.GetPermissions;

public sealed class GetPermissionsQueryHandler : IRequestHandler<GetPermissionsQuery, GetPermissionsResponse>
{
    private readonly IUnitOfWork _uow;

    public GetPermissionsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<GetPermissionsResponse> Handle(GetPermissionsQuery request, CancellationToken ct)
    {
        var permissions = await _uow.Permissions.GetAllAsync(ct);

        return new GetPermissionsResponse
        {
            Items = permissions.Select(p => new PermissionItemDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Module = p.Module,
                Description = p.Description
            }).ToList()
        };
    }
}
