using FluentValidation;

namespace AppBaseNetReact.Application.Features.Roles.Queries.GetRole;

public sealed class GetRoleQueryValidator : AbstractValidator<GetRoleQuery>
{
    public GetRoleQueryValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
    }
}
