using FluentAssertions;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Users.Queries.GetAvatar;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Features.Users.Queries.GetAvatar;

public class GetAvatarQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IFileStorageService> _storage = new();
    private readonly GetAvatarQueryHandler _handler;

    public GetAvatarQueryHandlerTests()
    {
        _handler = new GetAvatarQueryHandler(_uow.Object, _storage.Object);
    }

    [Fact]
    public async Task Handle_WithUserWithAvatar_ReturnsFilePath()
    {
        var user = User.Create("test@test.com", "John", "Doe", "hash123");
        user.SetAvatar("avatar123.jpg");
        _uow.Setup(x => x.Users.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _storage.Setup(x => x.GetFilePathAsync("avatar123.jpg"))
            .ReturnsAsync("/storage/avatars/avatar123.jpg");

        var outcome = await _handler.Handle(
            new GetAvatarQuery(user.Id), CancellationToken.None);

        outcome.Result.IsSuccess.Should().BeTrue();
        outcome.Result.FilePath.Should().Be("/storage/avatars/avatar123.jpg");
        outcome.Result.ContentType.Should().Be("image/jpeg");
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ReturnsUserNotFound()
    {
        _uow.Setup(x => x.Users.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var outcome = await _handler.Handle(
            new GetAvatarQuery(Guid.NewGuid()), CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be("UserNotFound");
    }

    [Fact]
    public async Task Handle_WithUserWithoutAvatar_ReturnsNoAvatar()
    {
        var user = User.Create("test@test.com", "John", "Doe", "hash123");
        _uow.Setup(x => x.Users.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var outcome = await _handler.Handle(
            new GetAvatarQuery(user.Id), CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be("NoAvatar");
    }

    [Fact]
    public async Task Handle_WithFileNotFound_ReturnsFileNotFound()
    {
        var user = User.Create("test@test.com", "John", "Doe", "hash123");
        user.SetAvatar("avatar123.jpg");
        _uow.Setup(x => x.Users.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _storage.Setup(x => x.GetFilePathAsync("avatar123.jpg"))
            .ReturnsAsync((string?)null);

        var outcome = await _handler.Handle(
            new GetAvatarQuery(user.Id), CancellationToken.None);

        outcome.Result.ErrorCode.Should().Be("FileNotFound");
    }

    [Theory]
    [InlineData(".png", "image/png")]
    [InlineData(".webp", "image/webp")]
    [InlineData(".jpg", "image/jpeg")]
    [InlineData(".jpeg", "image/jpeg")]
    public async Task Handle_WithDifferentExtensions_ReturnsCorrectContentType(string ext, string expectedContentType)
    {
        var user = User.Create("test@test.com", "John", "Doe", "hash123");
        var fileName = $"avatar{ext}";
        user.SetAvatar(fileName);
        _uow.Setup(x => x.Users.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _storage.Setup(x => x.GetFilePathAsync(fileName))
            .ReturnsAsync($"/storage/avatars/{fileName}");

        var outcome = await _handler.Handle(
            new GetAvatarQuery(user.Id), CancellationToken.None);

        outcome.Result.IsSuccess.Should().BeTrue();
        outcome.Result.ContentType.Should().Be(expectedContentType);
    }
}
