using FluentValidation;

namespace AppBaseNetReact.Application.Features.Users.Commands.ImportUsers;

public sealed class ImportUsersCommandValidator : AbstractValidator<ImportUsersCommand>
{
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    public ImportUsersCommandValidator()
    {
        RuleFor(x => x.FileContent)
            .NotNull().WithMessage("File content is required");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required")
            .Must(n => n.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only CSV files are accepted");
    }
}
