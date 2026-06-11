using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Common.Models;
using AppBaseNetReact.Application.Features.Auth.Commands.GoogleLogin;
using AppBaseNetReact.Domain.Entities;

namespace AppBaseNetReact.Application.Tests.Features.Auth.Commands.GoogleLogin;

public class GoogleLoginCommandHandlerTests
{
    private readonly Mock<IGoogleAuthService> _googleAuth = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IJwtService> _jwt = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<ILogger<GoogleLoginCommandHandler>> _logger = new();
    private readonly Mock<IRoleRepository> _roleRepo = new();
    private readonly GoogleLoginCommandHandler _handler;

    private readonly GoogleUserInfo _userInfo = new("12345", "user@gmail.com", "John", "Doe");
    private const string Code = "auth-code";
    private const string State = "nonce";

    public GoogleLoginCommandHandlerTests()
    {
        _clock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
        _uow.Setup(x => x.ExternalLogins.AddAsync(It.IsAny<ExternalLogin>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalLogin el, CancellationToken _) => el);
        _uow.Setup(x => x.Users.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) => u);
        _uow.Setup(x => x.RefreshTokens.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken t, CancellationToken _) => t);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _uow.SetupGet(x => x.Roles).Returns(_roleRepo.Object);
        _jwt.Setup(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<string>>()))
            .Returns(("access-token", DateTime.UtcNow.AddMinutes(15)));
        _jwt.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token");
        _jwt.Setup(x => x.HashRefreshToken("refresh-token")).Returns("refresh-hash");
        _googleAuth.Setup(x => x.ExchangeCodeAsync(Code, State, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_userInfo);

        _googleAuth.Setup(x => x.GetAuthorizationUrl(It.IsAny<string>())).Returns("https://accounts.google.com/o/oauth2/auth?client_id=xxx");

        _handler = new GoogleLoginCommandHandler(
            _googleAuth.Object, _uow.Object, _jwt.Object, _clock.Object, _logger.Object);
    }

    private static void SetBackingField<T>(object obj, string propertyName, T? value)
    {
        var field = obj.GetType().GetField($"<{propertyName}>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(obj, value);
    }

    private static User CreateActiveUser(string email = "user@gmail.com")
    {
        var user = User.Create(email, "John", "Doe", "hash", Guid.NewGuid());
        user.ConfirmEmail();
        return user;
    }

    private static Role CreatePublicRole()
    {
        return Role.Create("public", "Public role for OAuth users", true);
    }

    [Fact]
    public async Task Handle_NewUserViaGoogle_CreatesUserAndExternalLogin()
    {
        _uow.Setup(x => x.ExternalLogins.GetByProviderAsync("google", _userInfo.ProviderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalLogin?)null);
        _uow.Setup(x => x.Users.GetByEmailAsync(_userInfo.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _uow.Setup(x => x.Roles.GetByNameAsync("public", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        var outcome = await _handler.Handle(
            new GoogleLoginCommand(Code, State, "127.0.0.1", "Mozilla/5.0", "http://localhost:5173"),
            CancellationToken.None);

        outcome.Result.IsSuccess.Should().BeTrue();
        outcome.AccessToken.Should().Be("access-token");
        outcome.RefreshToken.Should().Be("refresh-token");
        _uow.Verify(x => x.Users.AddAsync(It.Is<User>(u =>
            u.Email == _userInfo.Email &&
            u.FirstName == _userInfo.FirstName &&
            u.LastName == _userInfo.LastName &&
            u.PasswordHash == null &&
            u.EmailConfirmed
        ), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.ExternalLogins.AddAsync(It.Is<ExternalLogin>(el =>
            el.Provider == "google" &&
            el.ProviderId == _userInfo.ProviderId
        ), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_ExistingUserByEmail_AutoLinksExternalLogin()
    {
        var existingUser = CreateActiveUser("user@gmail.com");
        var publicRole = CreatePublicRole();
        _roleRepo.Setup(x => x.GetByNameAsync("public", It.IsAny<CancellationToken>()))
            .ReturnsAsync(publicRole);
        _uow.Setup(x => x.ExternalLogins.GetByProviderAsync("google", _userInfo.ProviderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalLogin?)null);
        _uow.Setup(x => x.Users.GetByEmailAsync(_userInfo.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        var outcome = await _handler.Handle(
            new GoogleLoginCommand(Code, State, "127.0.0.1", "Mozilla/5.0", "http://localhost:5173"),
            CancellationToken.None);

        outcome.Result.IsSuccess.Should().BeTrue();
        outcome.AccessToken.Should().Be("access-token");
        _uow.Verify(x => x.Users.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(x => x.ExternalLogins.AddAsync(It.Is<ExternalLogin>(el =>
            el.Provider == "google" &&
            el.ProviderId == _userInfo.ProviderId &&
            el.UserId == existingUser.Id
        ), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_ExistingLinkedUser_ReturnsSuccessWithTokens()
    {
        var user = CreateActiveUser("user@gmail.com");
        var externalLogin = ExternalLogin.Create(user.Id, "google", _userInfo.ProviderId, user.Email);
        SetBackingField(externalLogin, "User", user);

        _uow.Setup(x => x.ExternalLogins.GetByProviderAsync("google", _userInfo.ProviderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(externalLogin);

        var outcome = await _handler.Handle(
            new GoogleLoginCommand(Code, State, "127.0.0.1", "Mozilla/5.0", "http://localhost:5173"),
            CancellationToken.None);

        outcome.Result.IsSuccess.Should().BeTrue();
        outcome.AccessToken.Should().Be("access-token");
        outcome.RefreshToken.Should().Be("refresh-token");
        _uow.Verify(x => x.Users.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(x => x.ExternalLogins.AddAsync(It.IsAny<ExternalLogin>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_InvalidState_ReturnsInvalidStateError()
    {
        _googleAuth.Setup(x => x.ExchangeCodeAsync(Code, State, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Invalid state"));

        var outcome = await _handler.Handle(
            new GoogleLoginCommand(Code, State, "127.0.0.1", "Mozilla/5.0", "http://localhost:5173"),
            CancellationToken.None);

        outcome.Result.IsSuccess.Should().BeFalse();
        outcome.Result.ErrorCode.Should().Be(GoogleLoginErrorCode.InvalidState);
        outcome.AccessToken.Should().BeNull();
    }

    [Fact]
    public async Task Handle_AuthFailed_ReturnsAuthFailedError()
    {
        _googleAuth.Setup(x => x.ExchangeCodeAsync(Code, State, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Google API error"));

        var outcome = await _handler.Handle(
            new GoogleLoginCommand(Code, State, "127.0.0.1", "Mozilla/5.0", "http://localhost:5173"),
            CancellationToken.None);

        outcome.Result.IsSuccess.Should().BeFalse();
        outcome.Result.ErrorCode.Should().Be(GoogleLoginErrorCode.AuthFailed);
        outcome.AccessToken.Should().BeNull();
    }
}
