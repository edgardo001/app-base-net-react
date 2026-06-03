# Middleware — Pipeline de Procesamiento HTTP

| Archivo | Propósito |
|---------|-----------|
| `ExceptionHandlingMiddleware.cs` | Captura global de excepciones no controladas → retorna `ApiResponse<T>` con código HTTP adecuado (500, 400, 404, etc.) y loguea el error |

## Orden en el pipeline

```
ExceptionHandling → SecurityHeaders → RateLimiting → CORS → Auth → Authorization → Controllers
```
