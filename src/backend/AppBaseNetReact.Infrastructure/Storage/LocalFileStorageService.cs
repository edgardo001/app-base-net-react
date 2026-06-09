using AppBaseNetReact.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace AppBaseNetReact.Infrastructure.Storage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly StorageOptions _options;

    public LocalFileStorageService(IOptions<StorageOptions> options)
    {
        _options = options.Value;
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string extension, CancellationToken ct = default)
    {
        var fileName = $"{Path.GetRandomFileName()}{extension}";
        var filePath = Path.Combine(_options.BasePath, fileName);

        Directory.CreateDirectory(_options.BasePath);

        using var output = new FileStream(filePath, FileMode.Create);
        await fileStream.CopyToAsync(output, ct);

        return fileName;
    }

    public Task<string?> GetFilePathAsync(string fileName)
    {
        var filePath = Path.Combine(_options.BasePath, fileName);
        return Task.FromResult(File.Exists(filePath) ? filePath : null);
    }

    public Task DeleteFileAsync(string fileName)
    {
        var filePath = Path.Combine(_options.BasePath, fileName);
        if (File.Exists(filePath))
            File.Delete(filePath);
        return Task.CompletedTask;
    }
}
