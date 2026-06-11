namespace AppBaseNetReact.WebApi.Middleware;

public class CsrfMiddleware
{
    private static readonly HashSet<string> _excludedMethods = ["GET", "HEAD", "OPTIONS", "TRACE"];
    private static readonly HashSet<string> _excludedPaths =
    [
        "/api/auth/login",
        "/api/auth/logout",
        "/api/auth/refresh",
        "/api/auth/forgot-password",
        "/api/auth/reset-password",
        "/api/auth/confirm-email",
        "/health",
        "/health/live",
        "/health/ready"
    ];

    private static bool IsPathExcluded(string? path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        var trimmed = path.TrimEnd('/');
        return _excludedPaths.Contains(trimmed) ||
               trimmed.StartsWith("/health/");
    }

    private readonly RequestDelegate _next;

    public CsrfMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_excludedMethods.Contains(context.Request.Method) &&
            !IsPathExcluded(context.Request.Path.Value))
        {
            if (!context.Request.Headers.TryGetValue("X-CSRF-TOKEN", out var token) ||
                string.IsNullOrWhiteSpace(token))
            {
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(
                    new { status = 403, message = "CSRF token missing. Include X-CSRF-TOKEN header." });
                return;
            }
        }

        await _next(context);
    }
}
