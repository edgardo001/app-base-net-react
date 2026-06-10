using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppBaseNetReact.Application.Common.Validators;
using AppBaseNetReact.Application.Features.Roles.Commands.CreateRole;
using AppBaseNetReact.Application.Features.Roles.Commands.UpdateRole;
using AppBaseNetReact.Application.Features.Roles.Commands.DeleteRole;
using AppBaseNetReact.Application.Features.Roles.Commands.UpdatePermissions;
using AppBaseNetReact.Application.Features.Roles.Queries.GetRoles;
using AppBaseNetReact.Application.Features.Roles.Queries.GetRole;
using AppBaseNetReact.Application.Features.Roles.Queries.GetUsersByRole;
using AppBaseNetReact.WebApi.Filters;

namespace AppBaseNetReact.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RolesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetRoles(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetRolesQuery(), ct);
        return Ok(ApiResponse<GetRolesResponse>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetRole(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetRoleQuery(id), ct);
        if (result == null) return NotFound();

        return Ok(ApiResponse<GetRoleResponse>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request, CancellationToken ct)
    {
        var outcome = await _mediator.Send(new CreateRoleCommand(
            request.Name, request.Description,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers.UserAgent.ToString()), ct);

        if (outcome.Result.ErrorCode == "DuplicateName")
            return Conflict(ApiResponse<object>.Fail(outcome.Result.ErrorMessage!));

        return CreatedAtAction(nameof(GetRole), new { id = outcome.Result.RoleId },
            ApiResponse<object>.Ok(new { outcome.Result.RoleId, outcome.Result.Name }));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken ct)
    {
        var outcome = await _mediator.Send(new UpdateRoleCommand(
            id, request.Name, request.Description,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers.UserAgent.ToString()), ct);

        if (outcome.Result.ErrorCode == "NotFound") return NotFound();
        if (outcome.Result.ErrorCode == "CannotModifySystemRole")
            return UnprocessableEntity(ApiResponse<object>.Fail(outcome.Result.ErrorMessage!));

        return Ok(ApiResponse<object>.Ok("Role updated"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteRole(Guid id, CancellationToken ct)
    {
        var outcome = await _mediator.Send(new DeleteRoleCommand(
            id, GetCurrentUserId(),
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers.UserAgent.ToString()), ct);

        if (outcome.Result.ErrorCode == "NotFound") return NotFound();
        if (outcome.Result.ErrorCode == "CannotDeleteSystemRole")
            return UnprocessableEntity(ApiResponse<object>.Fail(outcome.Result.ErrorMessage!));

        return Ok(ApiResponse<object>.Ok("Role deleted"));
    }

    [HttpPatch("{id:guid}/permissions")]
    public async Task<IActionResult> UpdatePermissions(Guid id, [FromBody] UpdatePermissionsRequest request, CancellationToken ct)
    {
        var outcome = await _mediator.Send(new UpdatePermissionsCommand(
            id,
            request.Permissions.Select(p => new PermissionAssignmentDto(p.PermissionId, p.Granted)).ToList(),
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers.UserAgent.ToString()), ct);

        if (outcome.Result.ErrorCode == "NotFound") return NotFound();

        return Ok(ApiResponse<object>.Ok("Permissions updated"));
    }

    [HttpGet("{id:guid}/users")]
    public async Task<IActionResult> GetUsersByRole(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetUsersByRoleQuery(id), ct);
        if (result == null) return NotFound();

        return Ok(ApiResponse<GetUsersByRoleResponse>.Ok(result));
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirst("sub")?.Value;
        if (Guid.TryParse(sub, out var id)) return id;
        return null;
    }
}
