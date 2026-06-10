using FluentValidation;

namespace AppBaseNetReact.Application.Features.Users.Commands.UploadAvatar;

public sealed class UploadAvatarCommandValidator : AbstractValidator<UploadAvatarCommand>
{
    public UploadAvatarCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.FileStream).NotNull();
        RuleFor(x => x.FileName).NotEmpty();
    }
}
