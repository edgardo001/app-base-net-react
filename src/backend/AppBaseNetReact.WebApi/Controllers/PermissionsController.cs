using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppBaseNetReact.Application.Features.Permissions.Queries.GetPermissions;
using AppBaseNetReact.Application.Features.Permissions.Queries.GetModules;
using AppBaseNetReact.WebApi.Filters;

namespace AppBaseNetReact.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PermissionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetPermissions(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPermissionsQuery(), ct);
        return Ok(ApiResponse<GetPermissionsResponse>.Ok(result));
    }

    [HttpGet("modules")]
    public async Task<IActionResult> GetModules(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetModulesQuery(), ct);
        return Ok(ApiResponse<GetModulesResponse>.Ok(result));
    }
}
