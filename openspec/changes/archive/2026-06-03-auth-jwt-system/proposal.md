## Why

Implementar autenticación enterprise-grade con JWT (access + refresh tokens), rotación de tokens, detección de reuse, rate limiting por endpoint, password hashing con PBKDF2, política de contraseñas configurable, y seguridad en capas (headers, middlewares). El planInicial.ia.md exige seguridad OWASP Top 10 compliant con refresh rotation, reuse detection y rate limiting granular.

## What Changes

- Implementación de JwtService con generación de access token (HS512, 15 min) y refresh token (64 bytes aleatorios, 7 días)
- Password hashing con PBKDF2 (SHA-256, 100k iteraciones, OWASP 2024)
- AuthController con endpoints: login, refresh (rotation + reuse detection), logout, change-password, forgot-password
- Rate limiting: Login (10/min), ForgotPassword (3/hr), Global (100/min)
- SecurityHeadersMiddleware con CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy, Permissions-Policy
- ExceptionHandlingMiddleware con mapeo de excepciones a HTTP status codes
- PasswordPolicyService con reglas configurables (longitud, mayúsculas, dígitos, especiales, expiración, lockout)
- AuditService para registro de eventos de seguridad (login, logout, cambio de clave, etc.)
- Refresh token rotation con reemplazo de hash anterior
- Reuse detection: si un token revocado se re-presenta, revocar todos los tokens del usuario

## Capabilities

### New Capabilities

- `jwt-authentication`: Generación y validación de JWT (HS512), claims estándar (sub, jti, email, permissions), clock skew configurable
- `refresh-token-management`: Rotación de refresh tokens, almacenamiento hasheado (SHA-256), detección de reuse, revocación
- `password-security`: Hashing PBKDF2, política de contraseñas (longitud, complejidad, expiración), lockout por intentos fallidos
- `api-rate-limiting`: Rate limiting por política (Login, ForgotPassword, Global) con fixed window, rejection status 429
- `security-headers`: Middleware de seguridad con CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy, Permissions-Policy
- `audit-logging`: Servicio de auditoría para eventos de seguridad y operaciones críticas
- `global-exception-handling`: Middleware de manejo global de excepciones con mapeo a respuestas HTTP estandarizadas

### Modified Capabilities

Ninguna — es la implementación inicial del sistema de autenticación.

## Impact

- Nuevos controladores: AuthController
- Nuevos servicios: JwtService, PasswordHasherService, PasswordPolicyService, AuditService
- Nuevos middlewares: ExceptionHandlingMiddleware, SecurityHeadersMiddleware
- Dependencias: Microsoft.AspNetCore.Authentication.JwtBearer, System.IdentityModel.Tokens.Jwt
- Rate limiting: 3 políticas registradas en pipeline antes de Authentication
