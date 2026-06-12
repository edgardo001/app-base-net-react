## Context

Ya existe una integración completa con Google OAuth2 que implementa el patrón: Controller → MediatR Command → Handler → Infrastructure Service + JWT. El frontend ya tiene una `OAuthCallbackPage` genérica que lee tokens del hash de la URL. Se reusará este mismo patrón y página para GitHub OAuth.

GitHub OAuth2 difiere de Google en que **no utiliza OpenID Connect** (no hay ID token JWT firmado). En su lugar:
1. Se redirige al usuario a `https://github.com/login/oauth/authorize`
2. GitHub devuelve un `code` de autorización
3. Se intercambia el `code` por un `access_token` via `POST https://github.com/login/oauth/access_token`
4. Se usa el `access_token` para llamar a `GET https://api.github.com/user` (y opcionalmente `GET https://api.github.com/user/emails`)
5. GitHub puede no exponer el email si el usuario lo tiene configurado como privado — se debe solicitar explícitamente el scope `user:email` y hacer una segunda llamada a la API de emails

## Goals / Non-Goals

**Goals:**
- Login con GitHub OAuth2 como nuevo proveedor
- Registro automático con rol `public` (reusando el existente)
- Reutilizar al máximo el patrón de Google OAuth (CQRS, controller, callback page)
- Documentación clara en README.md para configurar GitHub OAuth App

**Non-Goals:**
- Reemplazar Google OAuth ni login por email/password
- Migrar datos existentes
- Soportar más proveedores OAuth en este cambio

## Decisions

### 1. Flujo OAuth — Backend-driven (same as Google)
- **Decisión**: El frontend solo redirige a `/api/auth/github/login`; el backend maneja toda la lógica OAuth
- **Rationale**: Consistencia con Google OAuth, no exponer client secret en frontend, mismo patrón de callback

### 2. Estado CSRF vía ConcurrentDictionary (in-memory)
- **Decisión**: Misma estrategia que Google AuthService — `ConcurrentDictionary<string, string>` en memoria para el state parameter
- **Rationale**: Simple, consistente. Para un solo servidor es suficiente; si se escala horizontalmente se migraría a Redis/DB

### 3. Scopes de GitHub
- **Decisión**: Solicitar `read:user` (nombre, avatar, login) + `user:email` (email incluso si es privado)
- **Rationale**: GitHub no expone email sin `user:email` scope para usuarios con email privado, y necesitamos email para crear el usuario en el sistema

### 4. Manejo de email no disponible
- **Decisión**: Si GitHub no devuelve email ni en `/user` ni en `/user/emails`, se usa `{login}@github.local` como fallback
- **Rationale**: No podemos impedir el registro por falta de email; el usuario puede actualizarlo después en su perfil

### 5. Nombre del usuario
- **Decisión**: Usar `name` de GitHub si está disponible; si no, usar `login` como firstName y vacío como lastName
- **Rationale**: GitHub no exige nombre real; `login` es siempre único y disponible

### 6. Reutilización de OAuthCallbackPage
- **Decisión**: El frontend NO necesita cambios en `oauth-callback.tsx` — ya es genérica (lee tokens del hash)
- **Rationale**: El callback de GitHub y Google redirigen al mismo formato `#accessToken=...&refreshToken=...`

### 7. Rate Limiting
- **Decisión**: 10 requests/minuto por IP (mismo que Google)
- **Rationale**: Suficiente para uso normal, previene abuso

## Risks / Trade-offs

- **[GitHub API Rate Limits]**: GitHub tiene límites de 5000 req/hora autenticados. Si muchos usuarios intentan login simultáneamente podríamos agotar cuota. → **Mitigación**: El rate limiting propio (10 req/min/IP) previene esto en la práctica
- **[Email privado en GitHub]**: Si el usuario no autoriza `user:email` o la API de emails falla, usamos fallback `{login}@github.local`. → **Mitigación**: El usuario puede cambiar su email en la página de perfil
- **[Provider collision]**: Un usuario podría tener Google y GitHub vinculados al mismo email. → **Mitigación**: El handler trata cada provider por separado; la lógica actual de Google crea ExternalLogin por provider, por lo que es seguro
