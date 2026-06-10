using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Users.Commands.CreateUser;
using AppBaseNetReact.Domain.Entities;
using AppBaseNetReact.Infrastructure.Persistence;

namespace AppBaseNetReact.WebApi.Tests.Integration;

[Collection("Integration")]
public class UserCreationFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public UserCreationFlowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateUser_WhenAdminCreates_ReturnsSuccessWithUserId()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var newEmail = $"newuser-{Guid.NewGuid()}@example.com";

        // Act
        var command = new CreateUserCommand(
            newEmail, "New", "User", null,
            "http://localhost:5173", "127.0.0.1", "TestAgent");

        var outcome = await mediator.Send(command);

        // Assert
        outcome.Result.IsSuccess.Should().BeTrue();
        outcome.Result.UserId.Should().NotBeNull();
        outcome.Result.UserId!.Should().NotBe(Guid.Empty);
        outcome.Result.Email.Should().Be(newEmail);
    }

    [Fact]
    public async Task CreateUser_WhenCreated_HasForcePasswordChange()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var newEmail = $"forcepwd-{Guid.NewGuid()}@example.com";

        // Act
        var command = new CreateUserCommand(
            newEmail, "Force", "Password", null,
            "http://localhost:5173", "127.0.0.1", "TestAgent");

        var outcome = await mediator.Send(command);

        // Assert
        outcome.Result.IsSuccess.Should().BeTrue();

        var createdUser = await dbContext.Users.FirstAsync(u => u.NormalizedEmail == newEmail.ToUpperInvariant());
        createdUser.LastPasswordChangeAt.Should().BeNull("ForcePasswordChange sets LastPasswordChangeAt to null");
        createdUser.IsPasswordExpired().Should().BeTrue();
    }

    [Fact]
    public async Task CreateUser_WhenCreated_HasEmailConfirmationToken()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var newEmail = $"confirm-{Guid.NewGuid()}@example.com";

        // Act
        var command = new CreateUserCommand(
            newEmail, "Confirm", "Token", null,
            "http://localhost:5173", "127.0.0.1", "TestAgent");

        var outcome = await mediator.Send(command);

        // Assert
        outcome.Result.IsSuccess.Should().BeTrue();

        var createdUser = await dbContext.Users.FirstAsync(u => u.NormalizedEmail == newEmail.ToUpperInvariant());
        createdUser.EmailConfirmationToken.Should().NotBeNullOrEmpty();
        createdUser.EmailConfirmationTokenExpires.Should().NotBeNull();
        createdUser.EmailConfirmationTokenExpires!.Value.Should().BeAfter(DateTime.UtcNow);
        createdUser.EmailConfirmed.Should().BeFalse();
    }

    [Fact]
    public async Task CreateUser_WhenCreated_IsActiveByDefault()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var newEmail = $"active-{Guid.NewGuid()}@example.com";

        // Act
        var command = new CreateUserCommand(
            newEmail, "Active", "User", null,
            "http://localhost:5173", "127.0.0.1", "TestAgent");

        var outcome = await mediator.Send(command);

        // Assert
        outcome.Result.IsSuccess.Should().BeTrue();

        var createdUser = await dbContext.Users.FirstAsync(u => u.NormalizedEmail == newEmail.ToUpperInvariant());
        createdUser.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateUser_WhenDuplicateEmail_ReturnsFailure()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var duplicateEmail = $"dup-{Guid.NewGuid()}@example.com";

        // Create first user
        await mediator.Send(new CreateUserCommand(
            duplicateEmail, "First", "User", null,
            "http://localhost:5173", "127.0.0.1", "TestAgent"));

        // Act — try to create duplicate
        var outcome = await mediator.Send(new CreateUserCommand(
            duplicateEmail, "Second", "User", null,
            "http://localhost:5173", "127.0.0.1", "TestAgent"));

        // Assert
        outcome.Result.IsSuccess.Should().BeFalse();
        outcome.Result.ErrorCode.Should().Be("DuplicateEmail");
    }

    [Fact]
    public async Task CreateUser_WhenCreated_CanLoginWithCredentials()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasherService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var newEmail = $"loginnew-{Guid.NewGuid()}@example.com";
        var tempPassword = "TempPass123!";

        // Create user directly via entity (to control the password)
        var newUser = User.Create(newEmail, "Login", "New", hasher.HashPassword(tempPassword));
        newUser.ConfirmEmail();
        newUser.ForcePasswordChange();

        dbContext.Users.Add(newUser);
        await dbContext.SaveChangesAsync();

        var superAdminRole = await dbContext.Roles.FirstAsync(r => r.Name == "SuperAdmin");
        dbContext.UserRoles.Add(UserRole.Create(newUser.Id, superAdminRole.Id));
        await dbContext.SaveChangesAsync();

        // Act — login via HTTP
        using var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { Email = newEmail, Password = tempPassword });

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginApiResponse>();
        loginBody!.Success.Should().BeTrue();
        loginBody.Data!.PasswordExpired.Should().BeTrue();
    }

    private sealed class LoginApiResponse
    {
        public bool Success { get; set; }
        public LoginData? Data { get; set; }
    }

    private sealed class LoginData
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public bool PasswordExpired { get; set; }
    }
}
