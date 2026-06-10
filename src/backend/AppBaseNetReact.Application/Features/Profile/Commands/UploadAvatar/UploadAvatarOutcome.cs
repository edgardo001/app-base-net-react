namespace AppBaseNetReact.Application.Features.Profile.Commands.UploadAvatar;

public sealed record UploadAvatarOutcome(UploadAvatarResult Result)
{
    public string? FileName { get; init; }

    public static UploadAvatarOutcome Success(string fileName) =>
        new(UploadAvatarResult.Success()) { FileName = fileName };
    public static UploadAvatarOutcome UserNotFound() => new(UploadAvatarResult.UserNotFound());
    public static UploadAvatarOutcome InvalidExtension() => new(UploadAvatarResult.InvalidExtension());
    public static UploadAvatarOutcome FileTooLarge() => new(UploadAvatarResult.FileTooLarge());
}

public sealed record UploadAvatarResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorCode { get; init; }

    private UploadAvatarResult() { }

    public static UploadAvatarResult Success() => new() { IsSuccess = true };
    public static UploadAvatarResult UserNotFound() => new() { ErrorCode = "UserNotFound" };
    public static UploadAvatarResult InvalidExtension() => new() { ErrorCode = "InvalidExtension" };
    public static UploadAvatarResult FileTooLarge() => new() { ErrorCode = "FileTooLarge" };
}
