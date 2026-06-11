using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Infrastructure.Services;

namespace AppBaseNetReact.Application.Tests.Services;

public class PasswordPolicyServiceHistoryTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IPasswordHasherService> _hasherMock;
    private readonly PasswordPolicySettings _settings;
    private readonly PasswordPolicyService _service;

    public PasswordPolicyServiceHistoryTests()
    {
        _settings = new PasswordPolicySettings { PasswordHistoryCount = 3 };
        _uowMock = new Mock<IUnitOfWork>();
        _hasherMock = new Mock<IPasswordHasherService>();
        _service = new PasswordPolicyService(Options.Create(_settings), _uowMock.Object, _hasherMock.Object);
    }

    [Fact]
    public async Task CheckPasswordHistoryAsync_WhenPasswordMatchesRecentHash_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        _uowMock.Setup(x => x.PasswordHistories.GetRecentHashesAsync(userId, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "old-hash-1", "old-hash-2" });
        _hasherMock.Setup(h => h.VerifyPassword("matching-pwd", "old-hash-2")).Returns(true);

        var result = await _service.CheckPasswordHistoryAsync(userId, "matching-pwd", CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CheckPasswordHistoryAsync_WhenPasswordDoesNotMatchAnyHash_ReturnsTrue()
    {
        var userId = Guid.NewGuid();
        _uowMock.Setup(x => x.PasswordHistories.GetRecentHashesAsync(userId, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "old-hash-1", "old-hash-2" });
        _hasherMock.Setup(h => h.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var result = await _service.CheckPasswordHistoryAsync(userId, "new-unique-pwd", CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CheckPasswordHistoryAsync_WhenHistoryCountIsZero_SkipsCheck()
    {
        _settings.PasswordHistoryCount = 0;

        var result = await _service.CheckPasswordHistoryAsync(Guid.NewGuid(), "any-pwd", CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CheckPasswordHistoryAsync_WhenNoHistoryExists_ReturnsTrue()
    {
        var userId = Guid.NewGuid();
        _uowMock.Setup(x => x.PasswordHistories.GetRecentHashesAsync(userId, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var result = await _service.CheckPasswordHistoryAsync(userId, "any-pwd", CancellationToken.None);

        result.Should().BeTrue();
    }
}
