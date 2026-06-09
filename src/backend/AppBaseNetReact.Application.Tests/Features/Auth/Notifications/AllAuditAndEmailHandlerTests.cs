using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Auth.Notifications;
using AppBaseNetReact.Infrastructure.Notifications;

namespace AppBaseNetReact.Application.Tests.Features.Auth.Notifications;

public class UserLoggedInAuditHandlerTests
{
    private readonly Mock<IAuditService> _audit = new();
    private readonly UserLoggedInAuditHandler _handler;

    public UserLoggedInAuditHandlerTests()
    {
        _handler = new UserLoggedInAuditHandler(_audit.Object, Mock.Of<ILogger<UserLoggedInAuditHandler>>());
    }

    [Fact]
    public async Task Handle_CallsLogAsyncWithCorrectParams()
    {
        var notification = new UserLoggedInNotification(
            Guid.NewGuid(), "user@test.com", "127.0.0.1", "Mozilla/5.0");

        await _handler.Handle(notification, CancellationToken.None);

        _audit.Verify(x => x.LogAsync(
            "UserLoggedIn", "User", notification.UserId.ToString(),
            null, null, notification.UserId,
            notification.IpAddress, notification.UserAgent,
            It.Is<string>(d => d.Contains(notification.Email)),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class UserLoginFailedAuditHandlerTests
{
    private readonly Mock<IAuditService> _audit = new();
    private readonly UserLoginFailedAuditHandler _handler;

    public UserLoginFailedAuditHandlerTests()
    {
        _handler = new UserLoginFailedAuditHandler(_audit.Object, Mock.Of<ILogger<UserLoginFailedAuditHandler>>());
    }

    [Fact]
    public async Task Handle_CallsLogAsyncWithCorrectParams()
    {
        var notification = new UserLoginFailedNotification(
            "user@test.com", "127.0.0.1", "Invalid credentials");

        await _handler.Handle(notification, CancellationToken.None);

        _audit.Verify(x => x.LogAsync(
            "UserLoginFailed", "User", null,
            null, null, null,
            notification.IpAddress, "unknown",
            It.Is<string>(d => d.Contains(notification.Email) && d.Contains(notification.Reason)),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class TokenReuseDetectedAuditHandlerTests
{
    private readonly Mock<IAuditService> _audit = new();
    private readonly TokenReuseDetectedAuditHandler _handler;

    public TokenReuseDetectedAuditHandlerTests()
    {
        _handler = new TokenReuseDetectedAuditHandler(_audit.Object);
    }

    [Fact]
    public async Task Handle_CallsLogAsyncWithCorrectParams()
    {
        var notification = new TokenReuseDetectedNotification(
            Guid.NewGuid(), Guid.NewGuid(), "127.0.0.1", "Mozilla/5.0");

        await _handler.Handle(notification, CancellationToken.None);

        _audit.Verify(x => x.LogAsync(
            "TokenReuseDetected", "RefreshToken", notification.RefreshTokenId.ToString(),
            null, null, notification.UserId,
            notification.IpAddress, notification.UserAgent,
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class TokenRefreshedAuditHandlerTests
{
    private readonly Mock<IAuditService> _audit = new();
    private readonly TokenRefreshedAuditHandler _handler;

    public TokenRefreshedAuditHandlerTests()
    {
        _handler = new TokenRefreshedAuditHandler(_audit.Object);
    }

    [Fact]
    public async Task Handle_CallsLogAsyncWithCorrectParams()
    {
        var notification = new TokenRefreshedNotification(
            Guid.NewGuid(), "user@test.com", "127.0.0.1", "Mozilla/5.0");

        await _handler.Handle(notification, CancellationToken.None);

        _audit.Verify(x => x.LogAsync(
            "TokenRefreshed", "RefreshToken", null,
            null, null, notification.UserId,
            notification.IpAddress, notification.UserAgent,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class UserLoggedOutAuditHandlerTests
{
    private readonly Mock<IAuditService> _audit = new();
    private readonly UserLoggedOutAuditHandler _handler;

    public UserLoggedOutAuditHandlerTests()
    {
        _handler = new UserLoggedOutAuditHandler(_audit.Object);
    }

    [Fact]
    public async Task Handle_CallsLogAsyncWithCorrectParams()
    {
        var notification = new UserLoggedOutNotification(
            Guid.NewGuid(), "127.0.0.1", "Mozilla/5.0");

        await _handler.Handle(notification, CancellationToken.None);

        _audit.Verify(x => x.LogAsync(
            "UserLoggedOut", "RefreshToken", null,
            null, null, notification.UserId,
            notification.IpAddress, notification.UserAgent,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class PasswordChangedAuditHandlerTests
{
    private readonly Mock<IAuditService> _audit = new();
    private readonly PasswordChangedAuditHandler _handler;

    public PasswordChangedAuditHandlerTests()
    {
        _handler = new PasswordChangedAuditHandler(_audit.Object);
    }

    [Fact]
    public async Task Handle_CallsLogAsyncWithCorrectParams()
    {
        var notification = new PasswordChangedNotification(
            Guid.NewGuid(), "user@test.com", "Test", "127.0.0.1", "Mozilla/5.0");

        await _handler.Handle(notification, CancellationToken.None);

        _audit.Verify(x => x.LogAsync(
            "PasswordChanged", "User", notification.UserId.ToString(),
            null, null, notification.UserId,
            notification.IpAddress, notification.UserAgent,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class PasswordResetRequestedAuditHandlerTests
{
    private readonly Mock<IAuditService> _audit = new();
    private readonly PasswordResetRequestedAuditHandler _handler;

    public PasswordResetRequestedAuditHandlerTests()
    {
        _handler = new PasswordResetRequestedAuditHandler(_audit.Object);
    }

    [Fact]
    public async Task Handle_CallsLogAsyncWithCorrectParams()
    {
        var notification = new PasswordResetRequestedNotification(
            Guid.NewGuid(), "user@test.com", "Test", "http://reset.link", "127.0.0.1", "Mozilla/5.0");

        await _handler.Handle(notification, CancellationToken.None);

        _audit.Verify(x => x.LogAsync(
            "PasswordResetRequested", "User", notification.UserId.ToString(),
            null, null, notification.UserId,
            notification.IpAddress, notification.UserAgent,
            "Reset token generated",
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class PasswordResetAuditHandlerTests
{
    private readonly Mock<IAuditService> _audit = new();
    private readonly PasswordResetAuditHandler _handler;

    public PasswordResetAuditHandlerTests()
    {
        _handler = new PasswordResetAuditHandler(_audit.Object);
    }

    [Fact]
    public async Task Handle_CallsLogAsyncWithCorrectParams()
    {
        var notification = new PasswordResetNotification(
            Guid.NewGuid(), "user@test.com", "Test", "127.0.0.1", "Mozilla/5.0");

        await _handler.Handle(notification, CancellationToken.None);

        _audit.Verify(x => x.LogAsync(
            "PasswordReset", "User", notification.UserId.ToString(),
            null, null, notification.UserId,
            notification.IpAddress, notification.UserAgent,
            "Password reset via token",
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class EmailConfirmedAuditHandlerTests
{
    private readonly Mock<IAuditService> _audit = new();
    private readonly EmailConfirmedAuditHandler _handler;

    public EmailConfirmedAuditHandlerTests()
    {
        _handler = new EmailConfirmedAuditHandler(_audit.Object);
    }

    [Fact]
    public async Task Handle_CallsLogAsyncWithCorrectParams()
    {
        var notification = new EmailConfirmedNotification(
            Guid.NewGuid(), "user@test.com", "Test", "127.0.0.1", "Mozilla/5.0", "http://login.link");

        await _handler.Handle(notification, CancellationToken.None);

        _audit.Verify(x => x.LogAsync(
            "EmailConfirmed", "User", notification.UserId.ToString(),
            null, null, notification.UserId,
            notification.IpAddress, notification.UserAgent,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class AccountLockedEmailHandlerTests
{
    private readonly Mock<IEmailService> _email = new();
    private readonly AccountLockedEmailHandler _handler;

    public AccountLockedEmailHandlerTests()
    {
        _handler = new AccountLockedEmailHandler(_email.Object, Mock.Of<ILogger<AccountLockedEmailHandler>>());
    }

    [Fact]
    public async Task Handle_CallsSendAccountLockedEmailAsync()
    {
        var notification = new AccountLockedNotification(
            Guid.NewGuid(), "user@test.com", "Test", "127.0.0.1", 15, "http://frontend.com");

        await _handler.Handle(notification, CancellationToken.None);

        _email.Verify(x => x.SendAccountLockedEmailAsync(
            notification.Email,
            notification.FirstName,
            notification.LockoutMinutes,
            "http://frontend.com/reset-password",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailThrows_SwallowsException()
    {
        _email.Setup(x => x.SendAccountLockedEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SMTP down"));

        var notification = new AccountLockedNotification(
            Guid.NewGuid(), "user@test.com", "Test", "127.0.0.1", 15, "http://frontend.com");

        var act = async () => await _handler.Handle(notification, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}

public class PasswordResetRequestedEmailHandlerTests
{
    private readonly Mock<IEmailService> _email = new();
    private readonly PasswordResetRequestedEmailHandler _handler;

    public PasswordResetRequestedEmailHandlerTests()
    {
        _handler = new PasswordResetRequestedEmailHandler(_email.Object, Mock.Of<ILogger<PasswordResetRequestedEmailHandler>>());
    }

    [Fact]
    public async Task Handle_CallsSendPasswordResetEmailAsync()
    {
        var notification = new PasswordResetRequestedNotification(
            Guid.NewGuid(), "user@test.com", "Test", "http://reset.link", "127.0.0.1", "Mozilla/5.0");

        await _handler.Handle(notification, CancellationToken.None);

        _email.Verify(x => x.SendPasswordResetEmailAsync(
            notification.Email,
            notification.FirstName,
            notification.ResetLink,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailThrows_SwallowsException()
    {
        _email.Setup(x => x.SendPasswordResetEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SMTP down"));

        var notification = new PasswordResetRequestedNotification(
            Guid.NewGuid(), "user@test.com", "Test", "http://reset.link", "127.0.0.1", "Mozilla/5.0");

        var act = async () => await _handler.Handle(notification, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
