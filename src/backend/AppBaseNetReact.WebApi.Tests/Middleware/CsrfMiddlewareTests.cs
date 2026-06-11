using FluentAssertions;
using Microsoft.AspNetCore.Http;
using AppBaseNetReact.WebApi.Middleware;

namespace AppBaseNetReact.WebApi.Tests.Middleware;

public class CsrfMiddlewareTests
{
    private static async Task<(int StatusCode, string Body)> InvokeMiddleware(
        HttpContext context, RequestDelegate? next = null)
    {
        next ??= _ => Task.CompletedTask;
        var middleware = new CsrfMiddleware(next);
        var bodyStream = new MemoryStream();
        context.Response.Body = bodyStream;

        await middleware.InvokeAsync(context);

        bodyStream.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(bodyStream).ReadToEndAsync();
        return (context.Response.StatusCode, body);
    }

    private static HttpContext CreateContext(string method, string path, string? csrfToken = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        if (csrfToken != null)
            context.Request.Headers["X-CSRF-TOKEN"] = csrfToken;
        return context;
    }

    [Fact]
    public async Task PostRequest_WithoutCsrfToken_Returns403()
    {
        var context = CreateContext("POST", "/api/users");
        var (status, body) = await InvokeMiddleware(context);

        status.Should().Be(403);
        body.Should().Contain("CSRF token missing");
    }

    [Fact]
    public async Task PostRequest_WithValidCsrfToken_PassesThrough()
    {
        var context = CreateContext("POST", "/api/users", "some-csrf-token");
        var (status, _) = await InvokeMiddleware(context);

        status.Should().Be(200);
    }

    [Fact]
    public async Task PutRequest_WithoutCsrfToken_Returns403()
    {
        var context = CreateContext("PUT", "/api/users/123");
        var (status, _) = await InvokeMiddleware(context);

        status.Should().Be(403);
    }

    [Fact]
    public async Task DeleteRequest_WithoutCsrfToken_Returns403()
    {
        var context = CreateContext("DELETE", "/api/users/123");
        var (status, _) = await InvokeMiddleware(context);

        status.Should().Be(403);
    }

    [Fact]
    public async Task PatchRequest_WithoutCsrfToken_Returns403()
    {
        var context = CreateContext("PATCH", "/api/users/123/activate");
        var (status, _) = await InvokeMiddleware(context);

        status.Should().Be(403);
    }

    [Fact]
    public async Task GetRequest_WithoutCsrfToken_PassesThrough()
    {
        var context = CreateContext("GET", "/api/users");
        var (status, _) = await InvokeMiddleware(context);

        status.Should().Be(200);
    }

    [Fact]
    public async Task OptionsRequest_WithoutCsrfToken_PassesThrough()
    {
        var context = CreateContext("OPTIONS", "/api/users");
        var (status, _) = await InvokeMiddleware(context);

        status.Should().Be(200);
    }

    [Fact]
    public async Task LoginEndpoint_PostWithoutCsrf_PassesThrough()
    {
        var context = CreateContext("POST", "/api/auth/login");
        var (status, _) = await InvokeMiddleware(context);

        status.Should().Be(200);
    }

    [Fact]
    public async Task RefreshEndpoint_PostWithoutCsrf_PassesThrough()
    {
        var context = CreateContext("POST", "/api/auth/refresh");
        var (status, _) = await InvokeMiddleware(context);

        status.Should().Be(200);
    }

    [Fact]
    public async Task HealthEndpoint_GetWithoutCsrf_PassesThrough()
    {
        var context = CreateContext("GET", "/health");
        var (status, _) = await InvokeMiddleware(context);

        status.Should().Be(200);
    }

    [Fact]
    public async Task LogoutEndpoint_PostWithoutCsrf_PassesThrough()
    {
        var context = CreateContext("POST", "/api/auth/logout");
        var (status, _) = await InvokeMiddleware(context);

        status.Should().Be(200);
    }

    [Fact]
    public async Task ForgotPasswordEndpoint_PostWithoutCsrf_PassesThrough()
    {
        var context = CreateContext("POST", "/api/auth/forgot-password");
        var (status, _) = await InvokeMiddleware(context);

        status.Should().Be(200);
    }

    [Fact]
    public async Task ResetPasswordEndpoint_PostWithoutCsrf_PassesThrough()
    {
        var context = CreateContext("POST", "/api/auth/reset-password");
        var (status, _) = await InvokeMiddleware(context);

        status.Should().Be(200);
    }

    [Fact]
    public async Task ConfirmEmailEndpoint_PostWithoutCsrf_PassesThrough()
    {
        var context = CreateContext("POST", "/api/auth/confirm-email");
        var (status, _) = await InvokeMiddleware(context);

        status.Should().Be(200);
    }

    [Fact]
    public async Task HealthLiveEndpoint_WithoutCsrf_PassesThrough()
    {
        var context = CreateContext("GET", "/health/live");
        var (status, _) = await InvokeMiddleware(context);

        status.Should().Be(200);
    }

    [Fact]
    public async Task HealthReadyEndpoint_WithoutCsrf_PassesThrough()
    {
        var context = CreateContext("GET", "/health/ready");
        var (status, _) = await InvokeMiddleware(context);

        status.Should().Be(200);
    }

    [Fact]
    public async Task PostRequest_WithEmptyCsrfToken_Returns403()
    {
        var context = CreateContext("POST", "/api/users", "");
        var (status, _) = await InvokeMiddleware(context);

        status.Should().Be(403);
    }
}
