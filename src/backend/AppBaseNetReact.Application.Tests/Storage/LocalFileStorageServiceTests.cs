using FluentAssertions;
using Microsoft.Extensions.Options;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Infrastructure.Storage;

namespace AppBaseNetReact.Application.Tests.Storage;

public class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly LocalFileStorageService _service;

    public LocalFileStorageServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var options = Options.Create(new StorageOptions
        {
            BasePath = _tempDir,
            MaxFileSize = 1024,
            AllowedExtensions = [".jpg", ".png"]
        });
        _service = new LocalFileStorageService(options);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public async Task SaveFileAsync_CreatesFile_ReturnsFileName()
    {
        using var stream = new MemoryStream("test content"u8.ToArray());

        var fileName = await _service.SaveFileAsync(stream, ".jpg");

        fileName.Should().EndWith(".jpg");
        var filePath = Path.Combine(_tempDir, fileName);
        File.Exists(filePath).Should().BeTrue();
    }

    [Fact]
    public async Task GetFilePathAsync_WhenExists_ReturnsPath()
    {
        using var stream = new MemoryStream("content"u8.ToArray());
        var fileName = await _service.SaveFileAsync(stream, ".png");

        var result = await _service.GetFilePathAsync(fileName);

        result.Should().NotBeNull();
        result!.Should().EndWith(fileName);
    }

    [Fact]
    public async Task GetFilePathAsync_WhenNotExists_ReturnsNull()
    {
        var result = await _service.GetFilePathAsync("nonexistent.jpg");

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteFileAsync_RemovesFile()
    {
        using var stream = new MemoryStream("content"u8.ToArray());
        var fileName = await _service.SaveFileAsync(stream, ".jpg");

        await _service.DeleteFileAsync(fileName);

        File.Exists(Path.Combine(_tempDir, fileName)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteFileAsync_WhenNotExists_DoesNotThrow()
    {
        await _service.Invoking(s => s.DeleteFileAsync("nonexistent.jpg"))
            .Should().NotThrowAsync();
    }
}
