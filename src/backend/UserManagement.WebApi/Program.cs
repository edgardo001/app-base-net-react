using Serilog;
using Scalar.AspNetCore;
using System.Threading.RateLimiting;
using UserManagement.Application;
using UserManagement.Infrastructure;
using UserManagement.Infrastructure.Services;
using UserManagement.WebApi.Middleware;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day));

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Default", policy =>
        {
            policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
    });

    // Rate limiting
    var rateLimitConfig = builder.Configuration.GetSection("RateLimiting");
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = 429;

        options.AddPolicy("Login", ctx => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.Parse(rateLimitConfig["Login:Window"] ?? "00:01:00"),
                PermitLimit = int.Parse(rateLimitConfig["Login:MaxRequests"] ?? "10"),
                QueueLimit = int.Parse(rateLimitConfig["Login:QueueLimit"] ?? "0")
            }));

        options.AddPolicy("ForgotPassword", ctx => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.Parse(rateLimitConfig["ForgotPassword:Window"] ?? "01:00:00"),
                PermitLimit = int.Parse(rateLimitConfig["ForgotPassword:MaxRequests"] ?? "3"),
                QueueLimit = int.Parse(rateLimitConfig["ForgotPassword:QueueLimit"] ?? "0")
            }));

        options.AddPolicy("Global", ctx => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.Parse(rateLimitConfig["GlobalApi:Window"] ?? "00:01:00"),
                PermitLimit = int.Parse(rateLimitConfig["GlobalApi:MaxRequests"] ?? "100"),
                QueueLimit = int.Parse(rateLimitConfig["GlobalApi:QueueLimit"] ?? "2")
            }));
    });

    // Pipeline order (el orden importa):
    // 1. ExceptionHandling — catch errores de TODOS los middleware siguientes
    // 2. SecurityHeaders — headers en TODAS las respuestas (incluyendo errores)
    // 3. CORS — antes de auth porque preflight (OPTIONS) no necesita autenticacion
    // 4. RateLimiter — antes de auth para rechazar abusos sin gastar recursos de auth
    // 5. Authentication — establecer identidad del usuario
    // 6. Authorization — verificar permisos
    var app = builder.Build();

    await DatabaseSeeder.SeedAsync(app.Services);

    app.UseSerilogRequestLogging();
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<SecurityHeadersMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.UseCors("Default");
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    Log.Information("User Management API starting...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
