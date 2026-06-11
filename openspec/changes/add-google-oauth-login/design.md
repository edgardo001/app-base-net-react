## Context

Estado actual: autenticación solo por email/password con JWT + refresh token rotation. No existe infraestructura OAuth ni campos para proveedores externos en la entidad `User`. Los roles existentes son SuperAdmin, Admin, user-tipo-a/b/c — no hay rol "public". Frontend tiene login page con captcha, protected/authorized routes.

## Goals / Non-Goals

**Goals:**
- Agregar login con Google OAuth2 (Authorization Code Flow) sin modificar el flujo existente
- Auto-registro: primer login con Google crea usuario automáticamente
- Vincular automáticamente si el email ya existe en el sistema
- Nuevo rol `public` con permiso `page-public:view`
- Nueva página `/publico` con mensaje de bienvenida
- Instrucciones de configuración en README.md

**Non-Goals:**
- No se migran usuarios existentes a OAuth
- No se agregan otros proveedores OAuth (Facebook, GitHub, etc.)
- No se reemplaza el login por password

## Decisions

### D1: Authorization Code Flow (server-side)

**Decisión:** El backend inicia el flujo OAuth, Google redirige al callback del backend, el backend intercambia el code por tokens y redirige al frontend con el JWT en query params.

```
Frontend                    Backend                     Google
   │                          │                          │
   │  GET /api/auth/google    │                          │
   │─────────────────────────▶│                          │
   │        302 Redirect      │                          │
   │◀─────────────────────────│                          │
   │  Redirect a Google Auth  │                          │
   │────────────────────────────────────────────────────▶│
   │                          │                          │
   │            User authoriza en Google                  │
   │                          │                          │
   │                          │  GET /api/auth/callback  │
   │                          │◀─────────────────────────│
   │                          │   (authorization code)   │
   │                          │                          │
   │                          │  POST /token (code→tokens)│
   │                          │─────────────────────────▶│
   │                          │◀─────────────────────────│
   │                          │   (id_token, access_token)│
   │                          │                          │
   │                          │  Verify ID token         │
   │                          │  Find/Create user        │
   │                          │  Generate JWT            │
   │                          │                          │
   │  302 → /oauth-callback   │                          │
   │◀─────────────────────────│                          │
   │  #accessToken=xxx&       │                          │
   │  #refreshToken=yyy       │                          │
   │                          │                          │
```

**Rationale:** Más seguro que client-side flow porque el authorization code nunca se expone al frontend. El backend verifica el ID token con las claves públicas de Google (JWKS). Previene ataques de interceptación de token.

**Alternativa descartada:** ID token verification (frontend obtiene token, lo envía al backend). Menos seguro porque el token pasa por el cliente.

### D2: Entidad ExternalLogin en lugar de campos planos

**Decisión:** Se crea una nueva entidad `ExternalLogin` (UserId, Provider, ProviderId, ProviderEmail) con relación 1:N a User. No se agregan campos directamente en User.

**Rationale:** Soporta múltiples proveedores futuros (GitHub, Microsoft, etc.) sin modificar User. Sigue el patrón de ASP.NET Identity.

**Alternativa descartada:** Campo `GoogleId` en User — demasiado acoplado a un solo proveedor.

### D3: Rol "public" creado en seeder (IsSystem: true)

**Decisión:** El rol `public` se crea en el DatabaseSeeder con `IsSystem = true` (no se puede eliminar), con permiso `page-public:view`. Asignado automáticamente a usuarios que se registran vía Google OAuth.

**Rationale:** Consistente con los roles existentes SuperAdmin/Admin. Al ser IsSystem, previene eliminación accidental.

### D4: JWT return via URL hash fragment en redirect

**Decisión:** El backend genera JWT + refresh token y redirige al frontend a `/oauth-callback#accessToken=xxx&refreshToken=yyy&expiresAt=zzz`.

**Rationale:** Hash fragment no se envía al servidor en la petición HTTP, mayor seguridad. El frontend lee los tokens de `window.location.hash` y los almacena.

### D5: Vinculación automática por email

**Decisión:** Si el email del ID token de Google coincide con un usuario existente, se vincula la cuenta (se crea el `ExternalLogin`). No se requiere confirmación adicional.

**Rationale:** UX fluida. Riesgo bajo porque Google ya verificó la identidad del usuario.

### D6: Usuario creado sin password (passwordless)

**Decisión:** Los usuarios creados vía Google OAuth se crean con `PasswordHash = null`. El login por password no está disponible para ellos (el handler de login rechaza usuarios sin password). Solo pueden acceder vía Google.

**Rationale:** Simplifica la lógica. Un usuario creado vía Google no tiene password que recordar.

## Risks / Trade-offs

- **[Seguridad] Redirect URL abierta a manipulación** → Validar que el `state` parameter contenga un nonce generado por el backend (anti-CSRF)
- **[UX] JWT en URL puede quedar en historial** → Usar hash fragment + limpiar URL después de leer tokens
- **[Conflicto] Email existente con password** → Si el email ya existe, se vincula automáticamente. El usuario conserva ambos métodos de acceso. Riesgo: alguien podría reclamar un email que no le pertenece en Google. Mitigación: Google ya verificó la identidad.
- **[Rate limiting] Abuso del endpoint callback** → Aplicar rate limiting al callback OAuth
- **[Dependencia] Google como SPOF para estos usuarios** → Si Google está caído, estos usuarios no pueden acceder. Aceptado.
