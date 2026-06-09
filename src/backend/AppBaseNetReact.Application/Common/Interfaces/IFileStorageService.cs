namespace AppBaseNetReact.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string extension, CancellationToken ct = default);
    Task<string?> GetFilePathAsync(string fileName);
    Task DeleteFileAsync(string fileName);
}
