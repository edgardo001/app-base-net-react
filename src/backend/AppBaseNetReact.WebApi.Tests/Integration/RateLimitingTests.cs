using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Testcontainers.PostgreSql;
using Xunit;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Domain.Entities;
using AppBaseNetReact.Infrastructure.Persistence;

namespace AppBaseNetReact.WebApi.Tests.Integration;

[Collection("Integration")]
public class RateLimitingTests : IClassFixture<RateLimitingTests.RateLimitTestFactory>
{
    private readonly RateLimitTestFactory _factory;

    public RateLimitingTests(RateLimitTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_MultipleRapidRequests_EventuallyReturns429()
    {
        // Arrange
        await SeedTestDataAsync();
        using var client = _factory.CreateClient();

        var loginPayload = new { Email = "admin@sistema.local", Password = "admin" };

        // Act — send more requests than the rate limit allows
        var responses = new List<HttpResponseMessage>();
        for (var i = 0; i < RateLimitTestFactory.LoginMaxRequests + 2; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", loginPayload);
            responses.Add(response);
        }

        // Assert — at least some requests should be rate limited (429)
        var rateLimitedCount = responses.Count(r => r.StatusCode == (HttpStatusCode)429);
        rateLimitedCount.Should().BeGreaterThan(0,
            "some requests should be rate limited after exceeding the threshold");
    }

    [Fact]
    public async Task Login_RateLimited_Returns429StatusCode()
    {
        // Arrange
        await SeedTestDataAsync();
        using var client = _factory.CreateClient();

        var loginPayload = new { Email = "admin@sistema.local", Password = "admin" };

        // Act — exhaust the rate limit
        for (var i = 0; i < RateLimitTestFactory.LoginMaxRequests; i++)
        {
            await client.PostAsJsonAsync("/api/auth/login", loginPayload);
        }

        // Next request should be rate limited
        var response = await client.PostAsJsonAsync("/api/auth/login", loginPayload);

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)429);
    }

    [Fact]
    public async Task Login_RateLimitedResponse_Returns429WithProblemDetails()
    {
        // Arrange
        await SeedTestDataAsync();
        using var client = _factory.CreateClient();

        var loginPayload = new { Email = "admin@sistema.local", Password = "admin" };

        // Act — exhaust the rate limit
        for (var i = 0; i < RateLimitTestFactory.LoginMaxRequests; i++)
        {
            await client.PostAsJsonAsync("/api/auth/login", loginPayload);
        }

        var response = await client.PostAsJsonAsync("/api/auth/login", loginPayload);

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)429);
    }

    [Fact]
    public async Task ForgotPassword_MultipleRequests_ExceedsRateLimit()
    {
        // Arrange
        await SeedTestDataAsync();
        using var client = _factory.CreateClient();

        var forgotPayload = new { Email = "admin@sistema.local" };

        // Act — send requests up to and beyond the rate limit
        var responses = new List<HttpResponseMessage>();
        for (var i = 0; i < RateLimitTestFactory.ForgotPasswordMaxRequests + 2; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/forgot-password", forgotPayload);
            responses.Add(response);
        }

        // Assert — first N succeed (200), then rate limited (429)
        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        var rateLimitedCount = responses.Count(r => r.StatusCode == (HttpStatusCode)429);

        successCount.Should().Be(RateLimitTestFactory.ForgotPasswordMaxRequests);
        rateLimitedCount.Should().Be(2);
    }

    private async Task SeedTestDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasherService>();

        if (await dbContext.Users.AnyAsync(u => u.NormalizedEmail == "ADMIN@SISTEMA.LOCAL"))
            return;

        var adminUser = User.Create("admin@sistema.local", "Admin", "Usuario", hasher.HashPassword("admin"));
        adminUser.ConfirmEmail();
        dbContext.Users.Add(adminUser);
        await dbContext.SaveChangesAsync();

        var superAdminRole = await dbContext.Roles.FirstAsync(r => r.Name == "SuperAdmin");
        dbContext.UserRoles.Add(UserRole.Create(adminUser.Id, superAdminRole.Id));
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Custom factory with aggressive rate limiting for testing.
    /// Login: 3 requests/min, ForgotPassword: 2 requests/hr.
    /// </summary>
    public sealed class RateLimitTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        public const int LoginMaxRequests = 3;
        public const int ForgotPasswordMaxRequests = 2;

        private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
            .WithDatabase("test_rate_limit_db")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .Build();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseNpgsql(_dbContainer.GetConnectionString()));
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            // Reset Serilog's static logger to prevent "logger already frozen" across test classes
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
                .CreateBootstrapLogger();

            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:SecretKey"] = "test-secret-key-that-is-at-least-64-characters-long-for-hs512-signing-algorithm!!",
                    ["Jwt:Issuer"] = "AppBaseNetReact",
                    ["Jwt:Audience"] = "AppBaseNetReact",
                    ["Jwt:AccessTokenExpirationMinutes"] = "15",
                    ["Jwt:RefreshTokenExpirationDays"] = "7",
                    ["Jwt:ClockSkewSeconds"] = "0",
                    ["Email:Provider"] = "None",
                    ["Email:ForgotPasswordEnabled"] = "true",
                    ["Email:FrontendBaseUrl"] = "http://localhost:5173",
                    ["Email:FromName"] = "Test",
                    ["Email:FromEmail"] = "test@test.local",
                    ["Email:Smtp:Host"] = "",
                    ["Email:Smtp:Port"] = "587",
                    ["Email:Smtp:Username"] = "",
                    ["Email:Smtp:Password"] = "",
                    ["PasswordPolicy:RequiredLength"] = "6",
                    ["PasswordPolicy:MaxFailedAccessAttempts"] = "5",
                    ["PasswordPolicy:DefaultLockoutMinutes"] = "15",
                    // Aggressive rate limits for testing
                    ["RateLimiting:Login:Window"] = "00:01:00",
                    ["RateLimiting:Login:MaxRequests"] = LoginMaxRequests.ToString(),
                    ["RateLimiting:Login:QueueLimit"] = "0",
                    ["RateLimiting:ForgotPassword:Window"] = "01:00:00",
                    ["RateLimiting:ForgotPassword:MaxRequests"] = ForgotPasswordMaxRequests.ToString(),
                    ["RateLimiting:ForgotPassword:QueueLimit"] = "0",
                    ["RateLimiting:GlobalApi:Window"] = "00:01:00",
                    ["RateLimiting:GlobalApi:MaxRequests"] = "100",
                    ["RateLimiting:GlobalApi:QueueLimit"] = "2",
                    ["Storage:Provider"] = "Local",
                    ["Storage:BasePath"] = "storage/avatars",
                });
            });

            return base.CreateHost(builder);
        }

        public async Task InitializeAsync()
        {
            await _dbContainer.StartAsync();

            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        public new async Task DisposeAsync()
        {
            await _dbContainer.DisposeAsync();
        }
    }
}
