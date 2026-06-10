using FluentValidation;

namespace AppBaseNetReact.Application.Features.Roles.Queries.GetUsersByRole;

public sealed class GetUsersByRoleQueryValidator : AbstractValidator<GetUsersByRoleQuery>
{
    public GetUsersByRoleQueryValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
    }
}
