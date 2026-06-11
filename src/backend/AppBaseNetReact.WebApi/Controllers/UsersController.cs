using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppBaseNetReact.Application.Common.Models;
using AppBaseNetReact.Application.Common.Validators;
using AppBaseNetReact.Application.Features.Users.Commands.AdminResetPassword;
using AppBaseNetReact.Application.Features.Users.Commands.CreateUser;
using AppBaseNetReact.Application.Features.Users.Commands.DeleteUser;
using AppBaseNetReact.Application.Features.Users.Commands.RevokeTokens;
using AppBaseNetReact.Application.Features.Users.Commands.ToggleActive;
using AppBaseNetReact.Application.Features.Users.Commands.UpdateUser;
using AppBaseNetReact.Application.Features.Users.Commands.UploadAvatar;
using AppBaseNetReact.Application.Features.Users.Commands.ImportUsers;
using AppBaseNetReact.Application.Features.Users.Queries.ExportUsers;
using AppBaseNetReact.Application.Features.Users.Queries.GetAvatar;
using AppBaseNetReact.Application.Features.Users.Queries.GetUser;
using AppBaseNetReact.Application.Features.Users.Queries.GetUsers;
using AppBaseNetReact.WebApi.Filters;

namespace AppBaseNetReact.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly string _frontendUrl;

    public UsersController(IMediator mediator, IConfiguration configuration)
    {
        _mediator = mediator;
        _frontendUrl = configuration["FRONTEND_DOMAIN"] is { Length: > 0 } domain
            ? "https://" + domain
            : "http://localhost:5173";
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false,
        CancellationToken ct = default)
    {
        var response = await _mediator.Send(
            new GetUsersQuery(page, pageSize, search, sortBy, sortDesc), ct);

        return Ok(new PagedResponse<UserDto>
        {
            Items = response.Items,
            TotalCount = response.TotalCount,
            Page = response.Page,
            PageSize = response.PageSize,
            TotalPages = response.TotalPages,
            HasPreviousPage = response.HasPreviousPage,
            HasNextPage = response.HasNextPage
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken ct)
    {
        var response = await _mediator.Send(new GetUserQuery(id), ct);
        if (response == null) return NotFound();

        return Ok(ApiResponse<GetUserResponse>.Ok(response));
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var outcome = await _mediator.Send(new CreateUserCommand(
            request.Email,
            request.FirstName,
            request.LastName,
            request.RoleIds,
            _frontendUrl,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString()), ct);

        if (outcome.Result.IsSuccess)
            return CreatedAtAction(nameof(GetUser), new { id = outcome.Result.UserId },
                ApiResponse<object>.Ok(new { outcome.Result.UserId, outcome.Result.Email }));

        return outcome.Result.ErrorCode switch
        {
            "DuplicateEmail" => Conflict(ApiResponse<object>.Fail(outcome.Result.ErrorMessage!)),
            _ => BadRequest(ApiResponse<object>.Fail(outcome.Result.ErrorMessage!))
        };
    }

    [HttpPost("{id:guid}/resend-onboarding-email")]
    public async Task<IActionResult> ResendOnboardingEmail(Guid id, CancellationToken ct)
    {
        var command = new AppBaseNetReact.Application.Features.Users.Commands.ResendOnboardingEmail.ResendOnboardingEmailCommand(
            id,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        var outcome = await _mediator.Send(command, ct);

        return outcome.Result.ErrorCode switch
        {
            ResendOnboardingErrorCode.None => Ok(ApiResponse<object>.Ok("Onboarding email re-sent")),
            ResendOnboardingErrorCode.UserNotFound => NotFound(ApiResponse<object>.Fail(outcome.Result.ErrorMessage!)),
            ResendOnboardingErrorCode.AlreadyConfirmed => Conflict(ApiResponse<object>.Fail(outcome.Result.ErrorMessage!)),
            _ => BadRequest(ApiResponse<object>.Fail(outcome.Result.ErrorMessage!))
        };
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var outcome = await _mediator.Send(new UpdateUserCommand(
            id,
            request.FirstName,
            request.LastName,
            request.RoleIds,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString()), ct);

        if (outcome.Result.IsSuccess)
            return Ok(ApiResponse<object>.Ok("User updated"));

        return outcome.Result.ErrorCode switch
        {
            "UserNotFound" => NotFound(),
            _ => BadRequest(ApiResponse<object>.Fail(outcome.Result.ErrorMessage!))
        };
    }

    [HttpPatch("{id:guid}/activate")]
    public async Task<IActionResult> ToggleActive(Guid id, [FromBody] ToggleActiveRequest request, CancellationToken ct)
    {
        var outcome = await _mediator.Send(new ToggleActiveCommand(
            id,
            request.Active,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString()), ct);

        if (outcome.Result.IsSuccess)
            return Ok(ApiResponse<object>.Ok(request.Active ? "User activated" : "User deactivated"));

        return outcome.Result.ErrorCode switch
        {
            "UserNotFound" => NotFound(),
            _ => BadRequest(ApiResponse<object>.Fail(outcome.Result.ErrorMessage!))
        };
    }

    [HttpPatch("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, CancellationToken ct)
    {
        var loginLink = $"{Request.Scheme}://{Request.Host}/login";

        var outcome = await _mediator.Send(new AdminResetPasswordCommand(
            id,
            loginLink,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString()), ct);

        if (outcome.Result.IsSuccess)
            return Ok(ApiResponse<object>.Ok("Temporary password sent via email"));

        return outcome.Result.ErrorCode switch
        {
            "UserNotFound" => NotFound(),
            _ => BadRequest(ApiResponse<object>.Fail(outcome.Result.ErrorMessage!))
        };
    }

    [HttpPatch("{id:guid}/revoke-tokens")]
    public async Task<IActionResult> RevokeTokens(Guid id, CancellationToken ct)
    {
        var outcome = await _mediator.Send(new RevokeTokensCommand(
            id,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString()), ct);

        if (outcome.Result.IsSuccess)
            return Ok(ApiResponse<object>.Ok("All sessions revoked for user"));

        return outcome.Result.ErrorCode switch
        {
            "UserNotFound" => NotFound(),
            _ => BadRequest(ApiResponse<object>.Fail(outcome.Result.ErrorMessage!))
        };
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken ct)
    {
        var sub = User.FindFirst("sub")?.Value;
        Guid? currentUserId = Guid.TryParse(sub, out var parsed) ? parsed : null;

        var outcome = await _mediator.Send(new DeleteUserCommand(
            id,
            currentUserId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString()), ct);

        if (outcome.Result.IsSuccess)
            return Ok(ApiResponse<object>.Ok("User deleted"));

        return outcome.Result.ErrorCode switch
        {
            "UserNotFound" => NotFound(),
            "CannotDeleteSelf" => BadRequest(ApiResponse<object>.Fail(outcome.Result.ErrorMessage!)),
            _ => BadRequest(ApiResponse<object>.Fail(outcome.Result.ErrorMessage!))
        };
    }

    [HttpPost("{id:guid}/avatar")]
    [RequestSizeLimit(5242880)]
    public async Task<IActionResult> UploadAvatar(Guid id, IFormFile file, CancellationToken ct)
    {
        using var stream = file.OpenReadStream();
        var outcome = await _mediator.Send(new UploadAvatarCommand(
            id,
            stream,
            file.FileName,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers.UserAgent.ToString()), ct);

        if (outcome.Result.IsSuccess)
            return Ok(ApiResponse<object>.Ok(new { fileName = outcome.Result.FilePath }));

        return outcome.Result.ErrorCode switch
        {
            "UserNotFound" => NotFound(),
            "InvalidExtension" => BadRequest(ApiResponse<object>.Fail(outcome.Result.ErrorMessage!)),
            "FileTooLarge" => BadRequest(ApiResponse<object>.Fail(outcome.Result.ErrorMessage!)),
            _ => BadRequest(ApiResponse<object>.Fail(outcome.Result.ErrorMessage!))
        };
    }

    [HttpGet("{id:guid}/avatar")]
    public async Task<IActionResult> GetAvatar(Guid id, CancellationToken ct)
    {
        var outcome = await _mediator.Send(new GetAvatarQuery(id), ct);

        if (outcome.Result.IsSuccess)
            return PhysicalFile(outcome.Result.FilePath!, outcome.Result.ContentType!);

        return outcome.Result.ErrorCode switch
        {
            "UserNotFound" => NotFound(),
            "NoAvatar" => NotFound(ApiResponse<object>.Fail(outcome.Result.ErrorMessage!)),
            "FileNotFound" => NotFound(ApiResponse<object>.Fail(outcome.Result.ErrorMessage!)),
            _ => NotFound(ApiResponse<object>.Fail(outcome.Result.ErrorMessage!))
        };
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportUsers(
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false,
        CancellationToken ct = default)
    {
        var csv = await _mediator.Send(
            new ExportUsersQuery(search, sortBy, sortDesc), ct);
        return File(csv, "text/csv", "usuarios-export.csv");
    }

    [HttpPost("import")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> ImportUsers(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("No file uploaded"));

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse<object>.Fail("Only CSV files are accepted"));

        using var stream = file.OpenReadStream();
        var result = await _mediator.Send(new ImportUsersCommand(
            stream,
            file.FileName,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString()), ct);

        return Ok(ApiResponse<ImportUsersResult>.Ok(result));
    }
}

// Request types defined in AppBaseNetReact.Application.Common.Validators
