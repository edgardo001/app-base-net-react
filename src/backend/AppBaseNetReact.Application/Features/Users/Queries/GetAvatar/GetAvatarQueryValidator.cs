using FluentValidation;

namespace AppBaseNetReact.Application.Features.Users.Queries.GetAvatar;

public sealed class GetAvatarQueryValidator : AbstractValidator<GetAvatarQuery>
{
    public GetAvatarQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
