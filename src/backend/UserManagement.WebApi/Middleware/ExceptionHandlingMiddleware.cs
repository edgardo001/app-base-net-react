using System.Net;
using System.Text.Json;
using FluentValidation;
using Serilog;

namespace UserManagement.WebApi.Middleware;

// Middleware ubicado primero en el pipeline para capturar excepciones de TODOS los middleware
// registrados despues. Sin esto, excepciones en SecurityHeadersMiddleware, Auth, etc. devolverian
// 500 sin formato ApiResponse ni logging centralizado. Maneja ValidationException (400),
// UnauthorizedAccessException (403), KeyNotFoundException (404), y Exception generica (500).
// No expone detalles internos (stack trace, inner exception) por seguridad.
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";

            var response = new
            {
                StatusCode = 400,
                Message = "Validation failed",
                Errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
            };

            Log.Warning("Validation error: {Errors}", ex.Message);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        catch (UnauthorizedAccessException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                StatusCode = 403,
                Message = "Access denied"
            }));
        }
        catch (KeyNotFoundException ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                StatusCode = 404,
                Message = ex.Message
            }));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unhandled exception: {Message}", ex.Message);
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                StatusCode = 500,
                Message = "An internal server error occurred"
            }));
        }
    }
}
