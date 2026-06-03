using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagement.Application.Common.Interfaces;
using UserManagement.Application.Common.Validators;
using UserManagement.WebApi.Filters;

namespace UserManagement.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public PermissionsController(IUnitOfWork uow)
    {
        _uow = uow;
    }

    [HttpGet]
    public async Task<IActionResult> GetPermissions(CancellationToken ct)
    {
        var permissions = await _uow.Permissions.GetAllAsync(ct);
        return Ok(ApiResponse<object>.Ok(permissions.Select(p => new
        {
            p.Id, p.Code, p.Name, p.Module, p.Description
        })));
    }

    [HttpGet("modules")]
    public async Task<IActionResult> GetModules(CancellationToken ct)
    {
        var permissions = await _uow.Permissions.GetAllAsync(ct);
        var modules = permissions
            .GroupBy(p => p.Module)
            .Select(g => new
            {
                Module = g.Key,
                Permissions = g.Select(p => new { p.Id, p.Code, p.Name, p.Description })
            });
        return Ok(ApiResponse<object>.Ok(modules));
    }
}
