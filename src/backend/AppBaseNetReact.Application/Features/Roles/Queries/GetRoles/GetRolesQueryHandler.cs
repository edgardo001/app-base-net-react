using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;

namespace AppBaseNetReact.Application.Features.Roles.Queries.GetRoles;

public sealed class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, GetRolesResponse>
{
    private readonly IUnitOfWork _uow;

    public GetRolesQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<GetRolesResponse> Handle(GetRolesQuery request, CancellationToken ct)
    {
        var roles = await _uow.Roles.GetAllAsync(ct);

        return new GetRolesResponse
        {
            Items = roles.Select(r => new RoleListItemDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsSystem = r.IsSystem,
                CreatedAt = r.CreatedAt
            }).ToList()
        };
    }
}
