using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppBaseNetReact.Application.Common.Validators;
using AppBaseNetReact.Application.Features.Profile.Commands.UpdateProfile;
using AppBaseNetReact.Application.Features.Profile.Commands.UploadAvatar;
using AppBaseNetReact.Application.Features.Profile.Queries.GetActivity;
using AppBaseNetReact.Application.Features.Profile.Queries.GetProfile;
using AppBaseNetReact.WebApi.Filters;

namespace AppBaseNetReact.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProfileController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var result = await _mediator.Send(new GetProfileQuery(userId), ct);
        if (result == null) return NotFound();

        return Ok(ApiResponse<GetProfileResponse>.Ok(result));
    }

    [HttpGet("activity")]
    public async Task<IActionResult> GetActivity(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var result = await _mediator.Send(new GetActivityQuery(userId), ct);
        return Ok(ApiResponse<GetActivityResponse>.Ok(result));
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var outcome = await _mediator.Send(new UpdateProfileCommand(
            userId,
            request.FirstName,
            request.LastName,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers.UserAgent.ToString()), ct);

        if (!outcome.Result.IsSuccess)
        {
            return outcome.Result.ErrorCode switch
            {
                "UserNotFound" => NotFound(),
                _ => BadRequest(ApiResponse<object>.Fail("Profile update failed"))
            };
        }

        return Ok(ApiResponse<object>.Ok("Profile updated"));
    }

    [HttpPut("avatar")]
    [RequestSizeLimit(5242880)]
    public async Task<IActionResult> UploadAvatar(IFormFile file, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        var outcome = await _mediator.Send(
            new UploadAvatarCommand(userId, file.OpenReadStream(), ext, file.FileName), ct);

        if (!outcome.Result.IsSuccess)
        {
            return outcome.Result.ErrorCode switch
            {
                "UserNotFound" => NotFound(),
                "InvalidExtension" => BadRequest(ApiResponse<object>.Fail(
                    $"File type not allowed.")),
                "FileTooLarge" => BadRequest(ApiResponse<object>.Fail(
                    $"File size exceeds maximum allowed.")),
                _ => BadRequest(ApiResponse<object>.Fail("Avatar upload failed"))
            };
        }

        return Ok(ApiResponse<object>.Ok(new { fileName = outcome.FileName }));
    }
}
