using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;

namespace AppBaseNetReact.Application.Features.Permissions.Queries.GetModules;

public sealed class GetModulesQueryHandler : IRequestHandler<GetModulesQuery, GetModulesResponse>
{
    private readonly IUnitOfWork _uow;

    public GetModulesQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<GetModulesResponse> Handle(GetModulesQuery request, CancellationToken ct)
    {
        var permissions = await _uow.Permissions.GetAllAsync(ct);

        var modules = permissions
            .GroupBy(p => p.Module)
            .Select(g => new ModuleGroupDto
            {
                Module = g.Key,
                Permissions = g.Select(p => new ModulePermissionDto
                {
                    Id = p.Id,
                    Code = p.Code,
                    Name = p.Name,
                    Description = p.Description
                }).ToList()
            }).ToList();

        return new GetModulesResponse { Modules = modules };
    }
}
