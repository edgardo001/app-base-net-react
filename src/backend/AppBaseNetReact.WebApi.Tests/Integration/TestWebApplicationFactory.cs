using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Testcontainers.PostgreSql;
using AppBaseNetReact.Infrastructure.Persistence;

namespace AppBaseNetReact.WebApi.Tests.Integration;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithDatabase("test_db")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(ConnectionString));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Reset Serilog's static logger to prevent "logger already frozen" across test classes
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
            .CreateBootstrapLogger();

        // Add in-memory configuration to the HOST's IConfiguration so that
        // Program.Main's WebApplicationBuilder and AddInfrastructure can read it.
        // This is critical because AddInfrastructure reads JwtSettings directly
        // from IConfiguration to configure JWT bearer TokenValidationParameters.
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
                ["RateLimiting:Login:Window"] = "00:01:00",
                ["RateLimiting:Login:MaxRequests"] = "10",
                ["RateLimiting:Login:QueueLimit"] = "0",
                ["RateLimiting:ForgotPassword:Window"] = "01:00:00",
                ["RateLimiting:ForgotPassword:MaxRequests"] = "3",
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
        ConnectionString = _dbContainer.GetConnectionString();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }
}
