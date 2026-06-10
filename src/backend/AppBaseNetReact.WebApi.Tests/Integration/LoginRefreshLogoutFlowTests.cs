using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Features.Auth.Commands.Login;
using AppBaseNetReact.Application.Features.Auth.Commands.Refresh;
using AppBaseNetReact.Application.Features.Auth.Commands.Logout;
using AppBaseNetReact.Domain.Entities;
using AppBaseNetReact.Infrastructure.Persistence;

namespace AppBaseNetReact.WebApi.Tests.Integration;

[Collection("Integration")]
public class LoginRefreshLogoutFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public LoginRefreshLogoutFlowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokens()
    {
        // Arrange
        var (email, password) = await SeedTestUserAsync();

        // Act
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginApiResponse>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.AccessToken.Should().NotBeNullOrEmpty();
        body.Data.RefreshToken.Should().NotBeNullOrEmpty();
        body.Data.User.Should().NotBeNull();
        body.Data.User!.Email.Should().Be(email);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var (email, _) = await SeedTestUserAsync();

        // Act
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = "WrongPassword123!" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithUnconfirmedEmail_ReturnsForbidden()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasherService>();

        var testEmail = $"unconfirmed-{Guid.NewGuid()}@example.com";
        var password = "TestPass123!";

        var user = User.Create(testEmail, "Test", "User", hasher.HashPassword(password));
        // Email NOT confirmed — should cause 403
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Act
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { Email = testEmail, Password = password });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task FullFlow_LoginRefreshLogout_Succeeds()
    {
        // Arrange
        var (email, password) = await SeedTestUserAsync();

        using var client = _factory.CreateClient();

        // Step 1: Login
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginApiResponse>();
        var refreshToken = loginBody!.Data!.RefreshToken;

        // Step 2: Refresh
        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new { RefreshToken = refreshToken });
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshBody = await refreshResponse.Content.ReadFromJsonAsync<RefreshApiResponse>();
        refreshBody!.Success.Should().BeTrue();
        refreshBody.Data.Should().NotBeNull();
        refreshBody.Data!.AccessToken.Should().NotBeNullOrEmpty();
        refreshBody.Data.RefreshToken.Should().NotBeNullOrEmpty();
        // New refresh token should differ from the original (rotation)
        refreshBody.Data.RefreshToken.Should().NotBe(refreshToken);

        // Step 3: Logout with the new refresh token
        var logoutResponse = await client.PostAsJsonAsync("/api/auth/logout", new { RefreshToken = refreshBody.Data.RefreshToken });
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var logoutBody = await logoutResponse.Content.ReadFromJsonAsync<GenericApiResponse>();
        logoutBody!.Success.Should().BeTrue();

        // Step 4: Verify revoked token cannot be reused
        var reuseResponse = await client.PostAsJsonAsync("/api/auth/refresh", new { RefreshToken = refreshBody.Data.RefreshToken });
        reuseResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithRevokedToken_ReturnsUnauthorized()
    {
        // Arrange
        var (email, password) = await SeedTestUserAsync();
        using var client = _factory.CreateClient();

        // Login
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginApiResponse>();
        var refreshToken = loginBody!.Data!.RefreshToken;

        // Logout (revokes the token)
        await client.PostAsJsonAsync("/api/auth/logout", new { RefreshToken = refreshToken });

        // Act — try to use the revoked token
        var response = await client.PostAsJsonAsync("/api/auth/refresh", new { RefreshToken = refreshToken });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<(string Email, string Password)> SeedTestUserAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasherService>();

        var testEmail = $"test-{Guid.NewGuid()}@example.com";
        var password = "TestPass123!";

        var user = User.Create(testEmail, "Test", "User", hasher.HashPassword(password));
        user.ConfirmEmail();

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Assign SuperAdmin role (has all permissions)
        var superAdminRole = await dbContext.Roles.FirstAsync(r => r.Name == "SuperAdmin");
        dbContext.UserRoles.Add(UserRole.Create(user.Id, superAdminRole.Id));
        await dbContext.SaveChangesAsync();

        return (testEmail, password);
    }

    // Response DTOs matching the API's ApiResponse<T> wrapper
    private sealed class LoginApiResponse
    {
        public bool Success { get; set; }
        public LoginData? Data { get; set; }
        public string? Message { get; set; }
    }

    private sealed class LoginData
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
        public UserData? User { get; set; }
        public List<string>? Permissions { get; set; }
        public bool PasswordExpired { get; set; }
    }

    private sealed class UserData
    {
        public Guid UserId { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

    private sealed class RefreshApiResponse
    {
        public bool Success { get; set; }
        public RefreshData? Data { get; set; }
    }

    private sealed class RefreshData
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    private sealed class GenericApiResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }
}
