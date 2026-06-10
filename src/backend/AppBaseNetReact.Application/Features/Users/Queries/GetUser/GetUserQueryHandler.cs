using MediatR;
using AppBaseNetReact.Application.Common.Interfaces;

namespace AppBaseNetReact.Application.Features.Users.Queries.GetUser;

public sealed class GetUserQueryHandler : IRequestHandler<GetUserQuery, GetUserResponse?>
{
    private readonly IUnitOfWork _uow;

    public GetUserQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<GetUserResponse?> Handle(GetUserQuery request, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdWithRolesAsync(request.UserId, ct);
        if (user == null) return null;

        return new GetUserResponse
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
            Roles = user.UserRoles
                .Where(ur => ur.Role != null)
                .Select(ur => new RoleDto
                {
                    Id = ur.RoleId,
                    Name = ur.Role!.Name
                }).ToList()
        };
    }
}
