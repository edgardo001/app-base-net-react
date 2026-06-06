using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Common.Validators;
using AppBaseNetReact.Domain.Entities;
using AppBaseNetReact.WebApi.Filters;

namespace AppBaseNetReact.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;

    public RolesController(IUnitOfWork uow, IAuditService audit)
    {
        _uow = uow;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> GetRoles(CancellationToken ct)
    {
        var roles = await _uow.Roles.GetAllAsync(ct);
        return Ok(ApiResponse<List<RoleDetailDto>>.Ok(roles.Select(r => new RoleDetailDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            IsSystem = r.IsSystem,
            CreatedAt = r.CreatedAt
        }).ToList()));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetRole(Guid id, CancellationToken ct)
    {
        var role = await _uow.Roles.GetByIdWithPermissionsAsync(id, ct);
        if (role == null) return NotFound();

        return Ok(ApiResponse<object>.Ok(new
        {
            role.Id,
            role.Name,
            role.Description,
            role.IsSystem,
            role.CreatedAt,
            Permissions = role.RolePermissions.Select(rp => new
            {
                rp.Permission.Id,
                rp.Permission.Code,
                rp.Permission.Name,
                rp.Permission.Module,
                rp.Granted
            })
        }));
    }

    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request, CancellationToken ct)
    {
        var existing = await _uow.Roles.GetByNameAsync(request.Name, ct);
        if (existing != null)
            return Conflict(ApiResponse<object>.Fail("Role name already exists"));

        var role = Role.Create(request.Name, request.Description);
        await _uow.Roles.AddAsync(role, ct);
        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "RoleCreated", "Role", role.Id.ToString(),
            null, null, GetCurrentUserId(),
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers.UserAgent.ToString(),
            $"Role '{role.Name}' created", ct);

        return CreatedAtAction(nameof(GetRole), new { id = role.Id },
            ApiResponse<object>.Ok(new { role.Id, role.Name }));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken ct)
    {
        var role = await _uow.Roles.GetByIdAsync(id, ct);
        if (role == null) return NotFound();
        if (role.IsSystem)
            return UnprocessableEntity(ApiResponse<object>.Fail("Cannot modify system roles"));

        var oldName = role.Name;
        role.Update(request.Name, request.Description);
        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "RoleUpdated", "Role", role.Id.ToString(),
            System.Text.Json.JsonSerializer.Serialize(new { Name = oldName }),
            System.Text.Json.JsonSerializer.Serialize(new { role.Name, role.Description }),
            GetCurrentUserId(),
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers.UserAgent.ToString(),
            $"Role '{oldName}' updated to '{role.Name}'", ct);

        return Ok(ApiResponse<object>.Ok("Role updated"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteRole(Guid id, CancellationToken ct)
    {
        var role = await _uow.Roles.GetByIdAsync(id, ct);
        if (role == null) return NotFound();
        if (role.IsSystem)
            return UnprocessableEntity(ApiResponse<object>.Fail("Cannot delete system roles"));

        var roleName = role.Name;
        await _uow.Roles.DeleteAsync(role, ct);
        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "RoleDeleted", "Role", id.ToString(),
            null, null, GetCurrentUserId(),
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers.UserAgent.ToString(),
            $"Role '{roleName}' deleted", ct);

        return Ok(ApiResponse<object>.Ok("Role deleted"));
    }

    [HttpPatch("{id:guid}/permissions")]
    public async Task<IActionResult> UpdatePermissions(Guid id, [FromBody] UpdatePermissionsRequest request, CancellationToken ct)
    {
        var role = await _uow.Roles.GetByIdWithPermissionsAsync(id, ct);
        if (role == null) return NotFound();

        role.RolePermissions.Clear();

        foreach (var p in request.Permissions)
        {
            role.RolePermissions.Add(RolePermission.Create(id, p.PermissionId, p.Granted));
        }

        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "RolePermissionsUpdated", "Role", id.ToString(),
            null, System.Text.Json.JsonSerializer.Serialize(request.Permissions),
            GetCurrentUserId(),
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers.UserAgent.ToString(),
            $"Permissions updated for role '{role.Name}'", ct);

        return Ok(ApiResponse<object>.Ok("Permissions updated"));
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirst("sub")?.Value;
        if (Guid.TryParse(sub, out var id)) return id;
        return null;
    }
}

public class RoleDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Types defined in AppBaseNetReact.Application.Common.Validators
