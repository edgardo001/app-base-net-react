## Context

Sistema de autenticación enterprise para el gestor de usuarios. Se implementa JWT con refresh token rotation y reuse detection siguiendo las mejores prácticas de OWASP. El planInicial.ia.md define requisitos específicos de seguridad: algoritmo HS512, access token 15 min, refresh token 7 días, rotation en cada uso, detección de robo.

## Goals / Non-Goals

**Goals:**
- Autenticación stateless via JWT (access token) + refresh token con rotation
- Password hashing con PBKDF2 (OWASP 2024: 100k iteraciones, SHA-256, sal de 128 bits)
- Rate limiting por endpoint para prevenir ataques de fuerza bruta
- Seguridad en capas: headers HTTP, manejo global de excepciones, auditoría
- Refresh token rotation + reuse detection (sesión comprometida)

**Non-Goals:**
- 2FA / MFA (post-MVP)
- OAuth / SSO / LDAP (post-MVP)
- Envío de emails (no implementado aún)
- Captcha / Turnstile (pendiente)

## Decisions

### HS512 vs RS256
**Decisión:** HS512 (HMAC simétrico) para firma JWT.
**Alternativa considerada:** RS256 (asimétrico) con clave pública/privada.
**Razón:** Simplicidad operativa en un sistema monolite. RS256 añade complejidad de gestión de claves sin beneficio significativo cuando backend y frontend son servidos por el mismo equipo. Migrar a RS256 es sencillo cambiando la configuración JWT.

### PBKDF2 vs bcrypt vs Argon2id
**Decisión:** PBKDF2 con SHA-256 (100k iteraciones).
**Alternativa considerada:** Argon2id (más resistente a GPU/ASIC) o bcrypt.
**Razón:** PBKDF2 está disponible en .NET nativo (Rfc2898DeriveBytes) sin dependencias externas. Argon2id requeriría librería nativa (libsodium). 100k iteraciones supera la recomendación OWASP 2024 (72k). Se puede migrar a Argon2id en el futuro sin breaking change (el formato de almacenamiento identifica el algoritmo).

### Fixed window rate limiting
**Decisión:** FixedWindowRateLimiter (built-in de .NET).
**Alternativa considerada:** Sliding window o token bucket.
**Razón:** Simplicidad y bajo overhead. Fixed window es suficiente para los patrones de uso de este sistema. Las ventanas son cortas (1 minuto para login) lo que minimiza el efecto "burst en frontera de ventana".

### Refresh token rotation
**Decisión:** Rotation obligatoria en cada refresh (el anterior se revoca).
**Razón:** Previene el robo de tokens: si un token es interceptado y usado, el usuario legítimo recibe un error al refrescar (token ya revocado), lo que activa reuse detection. El planInicial.ia.md especifica explícitamente rotation + reuse detection.

### Security headers via middleware
**Decisión:** Middleware personalizado vs NWebsec.
**Alternativa considerada:** Paquete NWebsec.
**Razón:** Middleware de ~40 líneas evita una dependencia externa. CSP configurado para permitir Cloudflare Turnstile. Los headers se aplican via context.OnStarting() para asegurar que se incluyen incluso si middleware downstream modifica la respuesta.

## Risks / Trade-offs

- **Riesgo: Refresh token en localStorage** → Almacenar refresh token en localStorage del frontend es menos seguro que httpOnly cookies. Mitigación: el token en BD está hasheado, la rotation limita la ventana de exposición, reuse detection revoca todo si hay compromiso.
- **Riesgo: Forgot password sin email** → Actualmente devuelve la contraseña temporal en la respuesta HTTP. Mitigación: esto es temporal hasta que se implemente EmailService.
- **Riesgo: Fixed window rate limiting** → Un atacante puede hacer un burst justo después del reset de ventana. Mitigación: ventanas cortas (1 min) minimizan el impacto.
- **Riesgo: Sin Turnstile/Captcha** → Los endpoints de login son vulnerables a ataques automatizados. Mitigación: rate limiting + lockout por intentos fallidos + el Captcha está diseñado para agregarse sin breaking change.
