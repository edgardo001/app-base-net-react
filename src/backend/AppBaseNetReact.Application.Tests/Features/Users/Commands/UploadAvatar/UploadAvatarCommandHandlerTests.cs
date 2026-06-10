using FluentAssertions;
using MediatR;
using Moq;
using Microsoft.Extensions.Options;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Users.Commands.UploadAvatar;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Features.Users.Commands.UploadAvatar;

public class UploadAvatarCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IFileStorageService> _storage = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly UploadAvatarCommandHandler _handler;

    public UploadAvatarCommandHandlerTests()
    {
        var options = Options.Create(new StorageOptions
        {
            MaxFileSize = 5242880,
            AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"]
        });
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new UploadAvatarCommandHandler(_uow.Object, _storage.Object, options, _mediator.Object);
    }

    [Fact]
    public async Task Handle_WithValidFile_SavesAvatar()
    {
        var user = User.Create("test@test.com", "John", "Doe", "hash123");
        _uow.Setup(x => x.Users.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _storage.Setup(x => x.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("avatar123.jpg");

        var stream = new MemoryStream(new byte[100]);

        var outcome = await _handler.Handle(
            new UploadAvatarCommand(user.Id, stream, "avatar.jpg", "127.0.0.1", "TestAgent"), CancellationToken.None);

        outcome.Result.IsSuccess.Should().BeTrue();
        outcome.Result.FilePath.Should().Be("avatar123.jpg");
        user.AvatarPath.Should().Be("avatar123.jpg");
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidExtension_ReturnsInvalidExtension()
    {
        var user = User.Create("test@test.com", "John", "Doe", "hash123");
        _uow.Setup(x => x.Users.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var stream = new MemoryStream(new byte[100]);

        var outcome = await _handler.Handle(
            new UploadAvatarCommand(user.Id, stream, "malware.exe", "127.0.0.1", "TestAgent"), CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be("InvalidExtension");
        _storage.Verify(x => x.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithFileTooLarge_ReturnsFileTooLarge()
    {
        var user = User.Create("test@test.com", "John", "Doe", "hash123");
        _uow.Setup(x => x.Users.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var stream = new MemoryStream(new byte[6000000]);

        var outcome = await _handler.Handle(
            new UploadAvatarCommand(user.Id, stream, "large.jpg", "127.0.0.1", "TestAgent"), CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be("FileTooLarge");
        _storage.Verify(x => x.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ReturnsUserNotFound()
    {
        _uow.Setup(x => x.Users.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var stream = new MemoryStream(new byte[100]);

        var outcome = await _handler.Handle(
            new UploadAvatarCommand(Guid.NewGuid(), stream, "avatar.jpg", "127.0.0.1", "TestAgent"), CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be("UserNotFound");
        _storage.Verify(x => x.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
