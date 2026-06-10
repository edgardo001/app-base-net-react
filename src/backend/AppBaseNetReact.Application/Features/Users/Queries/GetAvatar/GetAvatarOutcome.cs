namespace AppBaseNetReact.Application.Features.Users.Queries.GetAvatar;

public sealed record GetAvatarOutcome(GetAvatarResult Result);

public sealed record GetAvatarResult(bool IsSuccess, string? FilePath = null, string? ContentType = null, string? ErrorCode = null, string? ErrorMessage = null)
{
    public static GetAvatarResult Success(string filePath, string contentType) => new(true, filePath, contentType);
    public static GetAvatarResult UserNotFound() => new(false, null, null, "UserNotFound", "User not found");
    public static GetAvatarResult NoAvatar() => new(false, null, null, "NoAvatar", "No avatar set");
    public static GetAvatarResult FileNotFound() => new(false, null, null, "FileNotFound", "Avatar file not found");
}
