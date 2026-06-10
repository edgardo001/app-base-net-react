# Controllers — Tests de Controladores

Pruebas unitarias para los controladores de la API. Validan códigos HTTP, estructura `ApiResponse<T>`, autorización, y mensajes de error.

| Archivo | Controlador |
|---------|-------------|
| ✅ `AuthControllerTests.cs` | AuthController |
| ✅ `UsersControllerTests.cs` | UsersController |
| ✅ `RolesControllerTests.cs` | RolesController |
| ✅ `PermissionsControllerTests.cs` | PermissionsController |
| ✅ `ProfileControllerTests.cs` | ProfileController |
| ✅ `AdminControllerTests.cs` | AdminController |

## Patrón

- Mock de `IMediator` (los controllers delegan en handlers CQRS)
- Mock de `HttpContext` + `ClaimsPrincipal` para endpoints autenticados
- Assert: `OkObjectResult`, `UnauthorizedResult`, `StatusCode(423)`, etc.
- Verificar mensaje de `ApiResponse<T>.Fail()`
