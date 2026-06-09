using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Common.Validators;
using AppBaseNetReact.WebApi.Filters;

namespace AppBaseNetReact.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;
    private readonly IFileStorageService _storage;
    private readonly StorageOptions _storageOptions;

    public ProfileController(IUnitOfWork uow, IAuditService audit, IFileStorageService storage, IOptions<StorageOptions> storageOptions)
    {
        _uow = uow;
        _audit = audit;
        _storage = storage;
        _storageOptions = storageOptions.Value;
    }

[HttpGet]
public async Task<IActionResult> GetProfile(CancellationToken ct)
{
    var userIdClaim = User.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        return Unauthorized();

    var user = await _uow.Users.GetByIdAsync(userId, ct);
    if (user == null)
        return NotFound();

    return Ok(ApiResponse<object>.Ok(new
    {
        user.Id,
        user.Email,
        user.FirstName,
        user.LastName,
        user.AvatarPath
    }));
}

[HttpGet("activity")]
public async Task<IActionResult> GetActivity(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var logs = await _uow.AuditLogs.GetByUserAsync(userId, 20, ct);
        var items = logs.Select(l => new
        {
            l.Action,
            l.EntityType,
            l.Details,
            l.CreatedAt
        });

        return Ok(ApiResponse<object>.Ok(items));
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _uow.Users.GetByIdAsync(userId, ct);
        if (user == null)
            return NotFound();

        var oldFirstName = user.FirstName;
        var oldLastName = user.LastName;
        var oldValues = $"{{\"firstName\":\"{oldFirstName}\",\"lastName\":\"{oldLastName}\"}}";
        var newValues = $"{{\"firstName\":\"{request.FirstName}\",\"lastName\":\"{request.LastName}\"}}";

        user.UpdateProfile(request.FirstName, request.LastName);
        await _uow.SaveChangesAsync(ct);

        await _audit.LogAsync(
            "ProfileUpdated", "User", user.Id.ToString(),
            oldValues, newValues, userId,
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            Request.Headers.UserAgent.ToString(), null, ct);

        return Ok(ApiResponse<object>.Ok("Profile updated"));
    }

    [HttpPut("avatar")]
    [RequestSizeLimit(5242880)]
    public async Task<IActionResult> UploadAvatar(IFormFile file, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _uow.Users.GetByIdAsync(userId, ct);
        if (user == null) return NotFound();

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_storageOptions.AllowedExtensions.Contains(ext))
            return BadRequest(ApiResponse<object>.Fail($"File type not allowed. Allowed: {string.Join(", ", _storageOptions.AllowedExtensions)}"));

        if (file.Length > _storageOptions.MaxFileSize)
            return BadRequest(ApiResponse<object>.Fail($"File size exceeds maximum of {_storageOptions.MaxFileSize} bytes"));

        var fileName = await _storage.SaveFileAsync(file.OpenReadStream(), ext, ct);
        user.SetAvatar(fileName);
        await _uow.SaveChangesAsync(ct);

        return Ok(ApiResponse<object>.Ok(new { fileName }));
    }
}
