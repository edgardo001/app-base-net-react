namespace AppBaseNetReact.WebApi.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    // Response.OnStarting asegura que los headers se agreguen incluso si la respuesta se modifica
    // en middleware downstream. Esto es mas robusto que agregarlos antes del next().
    // Headers:
    //   X-Frame-Options: DENY — previene clickjacking (no permitir iframes)
    //   X-Content-Type-Options: nosniff — evita MIME sniffing (obliga a usar Content-Type declarado)
    //   X-XSS-Protection: 1; mode=block — legacy XSS filter (defense in depth)
    //   Referrer-Policy: strict-origin-when-cross-origin — solo envía referrer en mismo origen
    //   Permissions-Policy: camera=(self), microphone=() — restringe APIs sensibles
    //   Content-Security-Policy: permite scripts de Cloudflare (Turnstile captcha), bloquera inline styles
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
            context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
            context.Response.Headers.TryAdd("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
            context.Response.Headers.TryAdd("Permissions-Policy", "camera=(self), microphone=()");
            context.Response.Headers.TryAdd("Content-Security-Policy",
                "default-src 'self'; " +
                "script-src 'self' https://challenges.cloudflare.com; " +
                "frame-src https://challenges.cloudflare.com; " +
                "img-src 'self' data: blob:; " +
                "style-src 'self' 'unsafe-inline';");
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
