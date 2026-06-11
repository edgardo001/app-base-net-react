using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Text;
using System.Threading.Channels;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Application.Common.Models;
using AppBaseNetReact.Infrastructure.Email;
using AppBaseNetReact.Infrastructure.Identity;
using AppBaseNetReact.Infrastructure.Persistence;
using AppBaseNetReact.Infrastructure.Persistence.Repositories;
using AppBaseNetReact.Infrastructure.Services;
using AppBaseNetReact.Infrastructure.Storage;

namespace AppBaseNetReact.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("PostgreSQL"),
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.MultipleCollectionIncludeWarning)));

        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.Configure<PasswordPolicySettings>(configuration.GetSection("PasswordPolicy"));
        services.Configure<EmailOptions>(configuration.GetSection("Email"));
        services.Configure<StorageOptions>(configuration.GetSection("Storage"));
        services.Configure<TurnstileOptions>(configuration.GetSection("Captcha"));
        services.Configure<GoogleOptions>(configuration.GetSection("Authentication:Google"));
        var emailOptions = configuration.GetSection("Email").Get<EmailOptions>()
            ?? throw new InvalidOperationException("Email settings not configured");
        if (string.IsNullOrWhiteSpace(emailOptions.FrontendBaseUrl))
            throw new InvalidOperationException(
                "Email:FrontendBaseUrl is required. Set it in appsettings.Production.json (e.g. https://your-domain.com).");
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasherService, PasswordHasherService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IPasswordPolicyService, PasswordPolicyService>();
        services.AddSingleton<IRandomPasswordGenerator, RandomPasswordGenerator>();
        services.AddSingleton<EmailRenderer>();
        services.AddSingleton(_ => Channel.CreateUnbounded<EmailMessage>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        }));
        services.AddHostedService<EmailBackgroundService>();
        services.AddSingleton<EmailQueueService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IEmailJob, EmailJob>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddHttpClient<ICaptchaService, TurnstileService>();
        services.AddHttpClient<IGoogleAuthService, GoogleAuthService>(client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("AppBaseNetReact/1.0");
        });

        var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>()
            ?? throw new InvalidOperationException("JWT settings not configured");

        // Limpia el mapeo de claims por defecto de Microsoft.IdentityModel.
        // Por defecto, "sub" se mapea a ClaimTypes.NameIdentifier, "email" a ClaimTypes.Email, etc.
        // Esto causa que los claims del JWT se pierdan o se mapeen incorrectamente.
        // Al limpiarlo, los claims se mantienen con sus nombres originales (JWT standard).
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ClockSkew = TimeSpan.FromSeconds(jwtSettings.ClockSkewSeconds)
            };
        });

        services.AddAuthorization();

        return services;
    }
}
