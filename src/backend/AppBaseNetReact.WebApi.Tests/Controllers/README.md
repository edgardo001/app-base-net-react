# Controllers — Tests de Controladores

Pruebas unitarias para los controladores de la API. Validan códigos HTTP, estructura `ApiResponse<T>`, autorización, y mensajes de error.

| Archivo | Controlador |
|---------|-------------|
| (pendiente) | AuthController |
| (pendiente) | UsersController |
| (pendiente) | RolesController |
| (pendiente) | PermissionsController |
| (pendiente) | ProfileController |
| (pendiente) | AdminController |

## Patrón

- Mock de `IUnitOfWork`, `IJwtService`, `IPasswordHasherService`, `IDateTimeProvider`, `IAuditService`, `IPasswordPolicyService`
- Mock de `HttpContext` + `ClaimsPrincipal` para endpoints autenticados
- Assert: `OkObjectResult`, `UnauthorizedResult`, `StatusCode(423)`, etc.
- Verificar mensaje de `ApiResponse<T>.Fail()`
