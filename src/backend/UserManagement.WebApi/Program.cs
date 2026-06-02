using Serilog;
using Scalar.AspNetCore;
using UserManagement.Application;
using UserManagement.Infrastructure;
using UserManagement.WebApi.Middleware;

// WebApi referencia Application e Infrastructure directamente (no solo
// Application como dicta la regla pura) porque AddInfrastructure() es
// un extension method definido en Infrastructure.DependencyInjection.
// La alternativa seria duplicar la configuracion DI en Application,
// pero eso rompe la separacion de responsabilidades (Application no
// debe conocer EF Core, JWT, etc.). La referencia directa es el
// approach pragmatico adoptado por la mayoria de proyectos .NET reales
// con arquitectura hexagonal (ver eShopOnContainers, CleanArchitecture).
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

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.UseCors("Default");
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
