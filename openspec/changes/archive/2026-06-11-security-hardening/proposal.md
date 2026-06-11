## Why

La política de contraseñas definida en `appsettings.json` incluye `PasswordHistoryCount: 5` (últimas 5 claves), pero nunca se verifica al cambiar la contraseña. Tampoco existe protección CSRF en las peticiones state-changing, lo cual es un requisito de OWASP Top 10 (A01) y del plan original. Ambos son vacíos de seguridad que deben cerrarse.

## What Changes

- **Password History**: Nueva entidad `PasswordHistory` en Domain, migración EF Core, y lógica en `ChangePasswordCommandHandler` que rechace usar una de las últimas N contraseñas
- **CSRF Protection**: Middleware/filtro que valide un header personalizado `X-CSRF-TOKEN` en peticiones POST/PUT/PATCH/DELETE, más integración en el frontend axios

## Capabilities

### New Capabilities
- `password-history`: Almacenar y verificar histórico de contraseñas para evitar reuso. Configurable via `PasswordHistoryCount`
- `csrf-protection`: Protección CSRF mediante header validation, configurable por ruta

### Modified Capabilities
Ninguna — no cambian requirements de specs existentes

## Impact

- **Backend**: Nueva entidad + migración; modificación de `ChangePasswordCommandHandler` e `IPasswordPolicyService`; nuevo middleware CSRF; registro en DI
- **Frontend**: Interceptor axios que añada `X-CSRF-TOKEN` en cada request state-changing
- **Tests**: Nuevos tests unitarios para handler, middleware, y frontend
