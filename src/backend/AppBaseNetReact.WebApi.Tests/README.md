# AppBaseNetReact.WebApi.Tests — Tests de la Capa de Presentación

## Propósito

Pruebas unitarias para los **controladores** de `WebApi`. Validan que los endpoints respondan correctamente (códigos HTTP, estructura `ApiResponse<T>`, autorización, rate limiting).

## Dependencias

- **Referencia:** `WebApi`
- **Framework:** xUnit + Moq + FluentAssertions

## Estructura

```
Controllers/     — Tests por controlador (AuthControllerTest, UsersControllerTest, etc.)
UnitTest1.cs     — Template inicial (reemplazar o eliminar)
```

## Patrón de测试

- Mock de `IUnitOfWork`, `IJwtService`, `IPasswordHasherService`, etc.
- Mock de `HttpContext` + `ClaimsPrincipal` para simular autenticación JWT
- Verificar código HTTP + estructura `ApiResponse<T>` + mensajes de error

## Convenciones

- Nomenclatura: `[Controller]_[Action]_[Escenario]_[ResultadoEsperado]`
- Cada test crea su propio controller con mocks frescos
- Probar casos: éxito, no autorizado, no encontrado, validación fallida, bloqueo, etc.

## Ejecución

```bash
dotnet test src/backend/AppBaseNetReact.WebApi.Tests
```
