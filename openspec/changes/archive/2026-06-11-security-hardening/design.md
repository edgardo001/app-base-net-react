## Context

El `PasswordPolicyService` ya define `PasswordHistoryCount = 5` pero no hay entidad, tabla ni lógica que lo implemente. El `ChangePasswordCommandHandler` solo valida formato de la nueva contraseña contra la política, pero no contra hashes anteriores. Tampoco existe protección CSRF: el plan original y OWASP Top 10 (A01: Broken Access Control) exigen validación de anti-forgery en peticiones state-changing.

## Goals / Non-Goals

**Goals:**
- Almacenar los últimos N hashes de contraseña por usuario (N configurable via `PasswordPolicyService.PasswordHistoryCount`)
- Rechazar cambio de contraseña si la nueva clave coincide con alguna de las últimas N almacenadas
- Implementar middleware CSRF que valide header `X-CSRF-TOKEN` en peticiones POST/PUT/PATCH/DELETE
- Frontend axios interceptor que envíe el token CSRF automáticamente
- Migración EF Core para la nueva tabla `PasswordHistories`

**Non-Goals:**
- No se implementa 2FA/MFA (Post-MVP)
- No se cambia el algoritmo de hashing (PBKDF2 se mantiene)
- No se modifican las políticas de expiración de contraseña existentes

## Decisions

### PasswordHistory como entidad separada vs JSON en User
- **Decisión**: Entidad separada `PasswordHistory` con FK a User
- **Rationale**: Permite consultas eficientes (top N por userId), evita bloobs JSON en la tabla User, y sigue el patrón de las demás entidades del dominio
- **Alternativa**: JSON column — más simple pero no indexable ni consultable

### CSRF via header vs cookie anti-forgery
- **Decisión**: Validación de header `X-CSRF-TOKEN` con un token aleatorio almacenado en sesión/claim
- **Rationale**: La app usa JWT en header (no cookies), por lo que el CSRF por cookie no aplica. El header custom es el estándar para SPAs con JWT
- **Alternativa**: `ValidateAntiForgeryToken` de ASP.NET Core — requiere cookies, no óptimo para SPA + JWT

### Middleware vs filtro vs endpoint attribute
- **Decisión**: Middleware global que verifique el header en rutas state-changing, con exclusión por ruta (ej: `/api/auth/login`)
- **Rationale**: No requiere modificar cada endpoint; se puede configurar por patrón de ruta

## Risks / Trade-offs

- [Risk] El histórico de contraseñas incrementa el tamaño de BD → Mitigación: solo almacenar hash (64 chars), límite configurable, cleanup automático al rotar
- [Risk] CSRF middleware podría bloquear peticiones legítimas si el frontend no envía el header → Mitigación: incluir excepción para endpoints de login/refresh, y probar exhaustivamente
- [Trade-off] Almacenar hashes anteriores vs re-hashear contra nueva contraseña: elegimos almacenar solo el hash para no poder revertirlo, el `PasswordHasher.Verify` puede comparar contra cada hash almacenado
