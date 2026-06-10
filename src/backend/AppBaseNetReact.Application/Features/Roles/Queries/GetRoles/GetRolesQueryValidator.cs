using FluentValidation;

namespace AppBaseNetReact.Application.Features.Roles.Queries.GetRoles;

public sealed class GetRolesQueryValidator : AbstractValidator<GetRolesQuery>
{
    public GetRolesQueryValidator()
    {
    }
}
