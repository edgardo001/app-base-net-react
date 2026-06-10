namespace AppBaseNetReact.Application.Features.Users.Commands.UploadAvatar;

public sealed record UploadAvatarOutcome(UploadAvatarResult Result);

public sealed record UploadAvatarResult(bool IsSuccess, string? FilePath = null, string? ErrorCode = null, string? ErrorMessage = null)
{
    public static UploadAvatarResult Success(string filePath) => new(true, filePath);
    public static UploadAvatarResult UserNotFound() => new(false, null, "UserNotFound", "User not found");
    public static UploadAvatarResult InvalidExtension(string allowedExtensions) => new(false, null, "InvalidExtension", $"File type not allowed. Allowed: {allowedExtensions}");
    public static UploadAvatarResult FileTooLarge(long maxFileSize) => new(false, null, "FileTooLarge", $"File size exceeds maximum of {maxFileSize} bytes");
}
