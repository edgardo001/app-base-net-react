// Controllers sin referencia a EF Core. Las operaciones de base de
// datos se hacen exclusivamente via IUnitOfWork (interfaces en
// Application layer). User.Create(...) se invoca con nombre fully
// qualified porque en .NET 10 con implicit usings, "User" puede
// resolver a System.IO.FileSystemAclExtensions en ciertos contextos,
// causando CS7036 (no es un bug del compilador sino una ambiguedad
// introducida por extension methods de System.IO en el SDK).
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Common.Validators;
using AppBaseNetReact.Domain.Entities;
using AppBaseNetReact.Infrastructure.Email;
using AppBaseNetReact.Infrastructure.Services;
using AppBaseNetReact.WebApi.Filters;

namespace AppBaseNetReact.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasherService _hasher;
    private readonly IEmailService _email;
    private readonly EmailRenderer _renderer;
    private readonly EmailOptions _emailOptions;

    public UsersController(
        IUnitOfWork uow,
        IPasswordHasherService hasher,
        IEmailService email,
        EmailRenderer renderer,
        IOptions<EmailOptions> emailOptions)
    {
        _uow = uow;
        _hasher = hasher;
        _email = email;
        _renderer = renderer;
        _emailOptions = emailOptions.Value;
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
        var result = await _uow.Users.GetPagedAsync(page, pageSize, null, sortBy, sortDesc, search, ct);
        return Ok(new PagedResponse<UserDto>
        {
            Items = result.Items.Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsActive = u.IsActive,
                EmailConfirmed = u.EmailConfirmed,
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt
            }).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages,
            HasPreviousPage = result.HasPreviousPage,
            HasNextPage = result.HasNextPage
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdWithRolesAsync(id, ct);
        if (user == null) return NotFound();

        return Ok(ApiResponse<UserDetailDto>.Ok(new UserDetailDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AvatarPath = user.AvatarPath,
            IsActive = user.IsActive,
            EmailConfirmed = user.EmailConfirmed,
            LastLoginAt = user.LastLoginAt,
            LastPasswordChangeAt = user.LastPasswordChangeAt,
            CreatedAt = user.CreatedAt,
            Roles = user.UserRoles.Select(ur => new RoleDto
            {
                Id = ur.RoleId,
                Name = ur.Role.Name
            }).ToList()
        }));
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var existing = await _uow.Users.GetByEmailAsync(request.Email, ct);
        if (existing != null)
            return Conflict(ApiResponse<object>.Fail("Email already registered"));

        var user = AppBaseNetReact.Domain.Entities.User.Create(
            request.Email,
            request.FirstName,
            request.LastName,
            _hasher.HashPassword(request.Password));

        if (request.RoleIds?.Any() == true)
        {
            foreach (var roleId in request.RoleIds)
                user.UserRoles.Add(UserRole.Create(user.Id, roleId));
        }

        await _uow.Users.AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);

        await SendEmail(user, "Welcome", new Dictionary<string, string>
        {
            ["UserName"] = user.FirstName,
            ["LoginLink"] = $"{Request.Scheme}://{Request.Host}/login"
        }, ct);

        return CreatedAtAction(nameof(GetUser), new { id = user.Id },
            ApiResponse<object>.Ok(new { user.Id, user.Email }));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdWithRolesAsync(id, ct);
        if (user == null) return NotFound();

        user.UpdateProfile(request.FirstName, request.LastName);

        if (request.RoleIds != null)
        {
            user.UserRoles.Clear();
            foreach (var roleId in request.RoleIds)
                user.UserRoles.Add(UserRole.Create(user.Id, roleId));
        }

        await _uow.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(null, "User updated"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(id, ct);
        if (user == null) return NotFound();

        await _uow.Users.DeleteAsync(user, ct);
        await _uow.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(null, "User deleted"));
    }

    [HttpPatch("{id:guid}/activate")]
    public async Task<IActionResult> ToggleActive(Guid id, [FromBody] ToggleActiveRequest request, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(id, ct);
        if (user == null) return NotFound();

        user.SetActive(request.Active);
        await _uow.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(null, request.Active ? "User activated" : "User deactivated"));
    }

    [HttpPatch("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(id, ct);
        if (user == null) return NotFound();

        var tempPassword = Guid.NewGuid().ToString("N")[..12];
        user.SetPasswordHash(_hasher.HashPassword(tempPassword));
        user.ConfirmEmail();
        await _uow.SaveChangesAsync(ct);

        await SendEmail(user, "TemporaryPassword", new Dictionary<string, string>
        {
            ["UserName"] = user.FirstName,
            ["TempPassword"] = tempPassword,
            ["LoginLink"] = $"{Request.Scheme}://{Request.Host}/login"
        }, ct);

        return Ok(ApiResponse<object>.Ok(null, "Temporary password sent via email"));
    }

    [HttpPatch("{id:guid}/revoke-tokens")]
    public async Task<IActionResult> RevokeTokens(Guid id, CancellationToken ct)
    {
        await _uow.RefreshTokens.RevokeAllForUserAsync(id, null, ct);
        await _uow.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(null, "All sessions revoked for user"));
    }

    private async Task SendEmail(Domain.Entities.User user, string templateName, Dictionary<string, string> extraVars, CancellationToken ct)
    {
        if (!_emailOptions.Templates.TryGetValue(templateName, out var config)) return;

        var vars = new Dictionary<string, string>(extraVars)
        {
            ["Year"] = DateTime.UtcNow.Year.ToString()
        };

        var htmlBody = _renderer.Render(config.TemplateFile, vars);
        await _email.SendEmailAsync(user.Email, config.Subject, htmlBody, ct);
    }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserDetailDto : UserDto
{
    public string? AvatarPath { get; set; }
    public DateTime? LastPasswordChangeAt { get; set; }
    public List<RoleDto> Roles { get; set; } = [];
}

public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public record ToggleActiveRequest(bool Active);
