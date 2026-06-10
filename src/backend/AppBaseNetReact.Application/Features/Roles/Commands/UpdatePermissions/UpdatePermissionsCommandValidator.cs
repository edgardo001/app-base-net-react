using FluentValidation;

namespace AppBaseNetReact.Application.Features.Roles.Commands.UpdatePermissions;

public sealed class UpdatePermissionsCommandValidator : AbstractValidator<UpdatePermissionsCommand>
{
    public UpdatePermissionsCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.Permissions).NotEmpty();
        RuleForEach(x => x.Permissions).ChildRules(p =>
        {
            p.RuleFor(x => x.PermissionId).NotEmpty();
        });
    }
}
