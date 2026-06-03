using FluentValidation;

namespace AppBaseNetReact.Application.Common.Validators;

public class UpdatePermissionsRequestValidator : AbstractValidator<UpdatePermissionsRequest>
{
    public UpdatePermissionsRequestValidator()
    {
        RuleFor(x => x.Permissions).NotEmpty();
        RuleForEach(x => x.Permissions).ChildRules(p =>
        {
            p.RuleFor(x => x.PermissionId).NotEmpty();
        });
    }
}

public record UpdatePermissionsRequest(List<PermissionAssignment> Permissions);
public record PermissionAssignment(Guid PermissionId, bool Granted);
