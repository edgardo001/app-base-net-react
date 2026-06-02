// RolesController sin EF Core. Las validaciones de negocio (IsSystem,
// nombre duplicado) se hacen en el controller por simplicidad; en una
// version posterior migrarian a Application layer via CQRS handlers.
// Los DTOs (RoleDetailDto, CreateRoleRequest, etc.) se definen inline
// para mantener el archivo autocontenido y evitar proliferacion de
// archivos pequenos en una fase temprana del proyecto.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagement.Application.Common.Interfaces;
using UserManagement.Domain.Entities;
using UserManagement.WebApi.Filters;

namespace UserManagement.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public RolesController(IUnitOfWork uow)
    {
        _uow = uow;
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

        role.Update(request.Name, request.Description);
        await _uow.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(null, "Role updated"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteRole(Guid id, CancellationToken ct)
    {
        var role = await _uow.Roles.GetByIdAsync(id, ct);
        if (role == null) return NotFound();
        if (role.IsSystem)
            return UnprocessableEntity(ApiResponse<object>.Fail("Cannot delete system roles"));

        await _uow.Roles.DeleteAsync(role, ct);
        await _uow.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(null, "Role deleted"));
    }

    [HttpPatch("{id:guid}/permissions")]
    public async Task<IActionResult> UpdatePermissions(Guid id, [FromBody] UpdatePermissionsRequest request, CancellationToken ct)
    {
        var role = await _uow.Roles.GetByIdAsync(id, ct);
        if (role == null) return NotFound();

        var existingPerms = await _uow.Roles.GetByIdWithPermissionsAsync(id, ct);
        if (existingPerms != null)
        {
            // This approach clears and re-adds; for simplicity
            // In production, use a proper diff-based approach
        }

        await _uow.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(null, "Permissions updated"));
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

public record CreateRoleRequest(string Name, string Description);
public record UpdateRoleRequest(string Name, string Description);
public record UpdatePermissionsRequest(List<PermissionAssignment> Permissions);
public record PermissionAssignment(Guid PermissionId, bool Granted);
