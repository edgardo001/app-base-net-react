using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using AppBaseNetReact.Application.Common.Interfaces;
using AppBaseNetReact.Infrastructure.Email;
using AppBaseNetReact.Infrastructure.Identity;
using AppBaseNetReact.Infrastructure.Persistence;
using AppBaseNetReact.Infrastructure.Persistence.Repositories;
using AppBaseNetReact.Infrastructure.Services;

namespace AppBaseNetReact.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("PostgreSQL"),
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.Configure<PasswordPolicySettings>(configuration.GetSection("PasswordPolicy"));
        services.Configure<EmailOptions>(configuration.GetSection("Email"));
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasherService, PasswordHasherService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IPasswordPolicyService, PasswordPolicyService>();
        services.AddSingleton<IRandomPasswordGenerator, RandomPasswordGenerator>();
        services.AddSingleton<EmailRenderer>();
        services.AddSingleton<EmailQueueService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IEmailJob, EmailJob>();

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
