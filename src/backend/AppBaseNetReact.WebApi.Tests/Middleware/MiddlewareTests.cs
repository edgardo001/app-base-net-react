using System.Net;
using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using AppBaseNetReact.WebApi.Middleware;

namespace AppBaseNetReact.WebApi.Tests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenNoException_CallsNext()
    {
        var nextCalled = false;
        var middleware = new ExceptionHandlingMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenValidationException_Returns400()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var validationErrors = new List<ValidationFailure>
        {
            new("Email", "Email is required")
        };

        var middleware = new ExceptionHandlingMiddleware(_ =>
            throw new ValidationException(validationErrors));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(400);
        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        body.Should().Contain("Validation failed");
    }

    [Fact]
    public async Task InvokeAsync_WhenUnauthorizedAccessException_Returns403()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(_ =>
            throw new UnauthorizedAccessException("Forbidden"));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(403);
        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        body.Should().Contain("Access denied");
    }

    [Fact]
    public async Task InvokeAsync_WhenKeyNotFoundException_Returns404()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(_ =>
            throw new KeyNotFoundException("Resource not found"));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(404);
        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        body.Should().Contain("Resource not found");
    }

    [Fact]
    public async Task InvokeAsync_WhenGenericException_Returns500()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(_ =>
            throw new InvalidOperationException("Something went wrong"));

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(500);
        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        body.Should().Contain("An internal server error occurred");
        body.Should().NotContain("Something went wrong");
    }
}

public class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_CallsNext()
    {
        var nextCalled = false;
        var middleware = new SecurityHeadersMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_DoesNotThrow()
    {
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();

        var act = async () => await middleware.InvokeAsync(context);

        await act.Should().NotThrowAsync();
    }
}
