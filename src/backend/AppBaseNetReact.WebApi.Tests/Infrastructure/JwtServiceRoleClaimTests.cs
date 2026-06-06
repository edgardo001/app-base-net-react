using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Options;
using AppBaseNetReact.Domain.Entities;
using AppBaseNetReact.Infrastructure.Identity;

namespace AppBaseNetReact.WebApi.Tests.Infrastructure;

public class JwtServiceRoleClaimTests
{
    [Fact]
    public void GenerateAccessToken_EmitsRoleClaim_UnderLongUri_NotShortName()
    {
        // Documents the JWT payload structure: the role claim is written
        // under the long URI (http://schemas.microsoft.com/ws/2008/06/identity/claims/role),
        // NOT under the short "role" key. .NET 10's JwtSecurityTokenHandler
        // does not rewrite ClaimTypes.Role on write the way the .NET
        // Framework handler did, so the URI ends up verbatim in the
        // payload. The frontend decoder (src/frontend/src/lib/jwt.ts)
        // reads the long URI. If this test fails, the decoder must be
        // updated to match the new key.
        var settings = Options.Create(new JwtSettings
        {
            SecretKey = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
            Issuer = "https://app",
            Audience = "https://app",
            AccessTokenExpirationMinutes = 15,
        });
        var jwt = new JwtService(settings);

        var user = User.Create("u@test.com", "T", "U", "h");
        var adminRole = Role.Create("Admin", "Admin role");
        var superAdminRole = Role.Create("SuperAdmin", "SuperAdmin role");
        user.UserRoles.Add(BuildUserRole(user.Id, adminRole));
        user.UserRoles.Add(BuildUserRole(user.Id, superAdminRole));

        var (token, _) = jwt.GenerateAccessToken(user, permissions: []);

        // Decode the payload (middle segment) without verifying signature.
        var parts = token.Split('.');
        parts.Should().HaveCount(3);
        var payloadJson = Encoding.UTF8.GetString(Convert.FromBase64String(
            parts[1].Replace('-', '+').Replace('_', '/').PadRight(
                parts[1].Length + ((4 - parts[1].Length % 4) % 4), '=')));
        using var doc = JsonDocument.Parse(payloadJson);
        var root = doc.RootElement;

        var longNameRole = root.TryGetProperty(
            "http://schemas.microsoft.com/ws/2008/06/identity/claims/role", out var longProp)
            ? longProp.ValueKind == JsonValueKind.Array
                ? string.Join(",", longProp.EnumerateArray().Select(e => e.GetString()))
                : longProp.GetString()
            : null;

        var shortNameRole = root.TryGetProperty("role", out var shortProp)
            ? shortProp.ValueKind == JsonValueKind.Array
                ? string.Join(",", shortProp.EnumerateArray().Select(e => e.GetString()))
                : shortProp.GetString()
            : null;

        longNameRole.Should().NotBeNull(
            "role claim MUST be under the long URI; the frontend decoder reads from this key");
        longNameRole!.Should().Contain("Admin").And.Contain("SuperAdmin");
        shortNameRole.Should().BeNull(
            "role claim MUST NOT be under the short 'role' key in .NET 10; if it appears here, " +
            "the runtime has changed behavior and the frontend decoder must be updated");
    }

    private static UserRole BuildUserRole(Guid userId, Role role)
    {
        // UserRole.Role has a private setter; in production EF Core sets
        // it via the Include() projection. In a unit test we use
        // reflection to populate the navigation property.
        var ur = UserRole.Create(userId, role.Id);
        typeof(UserRole).GetProperty(nameof(UserRole.Role))!
            .SetValue(ur, role);
        return ur;
    }
}
