namespace AppBaseNetReact.Application.Common.Interfaces;

public class StorageOptions
{
    public string Provider { get; set; } = "Local";
    public string BasePath { get; set; } = "storage/avatars";
    public long MaxFileSize { get; set; } = 5242880;
    public string[] AllowedExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".webp"];
}
