# Plan Integral — Sistema de Gestión de Usuarios Enterprise

## 1. Stack Tecnológico

| Capa | Tecnología | Versión | Licencia |
|------|-----------|---------|----------|
| Backend | .NET | 10.0 | MIT |
| Frontend | React 19 + Vite | 19.x | MIT |
| UI Framework | Tailwind CSS + shadcn/ui | 4.x | MIT |
| BD Relacional | PostgreSQL | 16+ | PostgreSQL |
| ORM | Entity Framework Core 10 | 10.x | MIT |
| Autenticación | JWT (Access + Refresh Token) | — | — |
| Logs | Serilog (estructurado) | — | MIT |
| Envío de correos | MailKit + MimeKit | — | MIT |
| Testing | xUnit + Moq + FluentAssertions + Testcontainers | — | MIT |
| Contenedorización | Docker + Docker Compose | — | Apache 2.0 |
| Proxy inverso | Traefik | — | MIT |
| DNS/CDN/Security | Cloudflare (DNS, Turnstile, WAF) | — | — |
| OpenAPI | Scalar UI (Scalar.AspNetCore) | — | MIT |
| Validación | FluentValidation | — | Apache 2.0 |
| Mapeo | AutoMapper | — | MIT |
| Rate Limiting | AspNetCore RateLimiting (integrado) | — | MIT |
| Seguridad headers | NWebsec o middleware personalizado | — | MIT |
| Background Jobs | Quartz.NET (envío de correos, tareas programadas) | — | Apache 2.0 |
| Monitoreo | Health Checks + Prometheus / Grafana (opcional) | — | MIT / Apache 2.0 |

## 2. Arquitectura del Proyecto (Hexagonal + Vertical Slicing)

```
netReactMVP/
├── backend/
│   └── UserManagement.sln
│       ├── src/
│       │   ├── UserManagement.Domain/           # Entidades, Value Objects, Enums, Domain Events
│       │   ├── UserManagement.Application/      # Casos de uso, DTOs, Interfaces de puertos
│       │   │   ├── Common/                      # Behaviors, Exceptions, Mappings
│       │   │   ├── Features/
│       │   │   │   ├── Auth/                    # Login, Register, Refresh, Logout, ForgotPassword
│       │   │   │   ├── Users/                   # CRUD, gestión de usuarios
│       │   │   │   ├── Roles/                   # CRUD de roles
│       │   │   │   ├── Profile/                 # Perfil propio del usuario
│       │   │   │   └── Admin/                   # Dashboard, auditoría
│       │   │   └── Interfaces/                  # Puertos: repositorios, servicios
│       │   ├── UserManagement.Infrastructure/   # Implementaciones concretas
│       │   │   ├── Persistence/                 # DbContext, Migrations, Repositorios
│       │   │   ├── Identity/                    # Password hasher, JWT provider, Token store
│       │   │   ├── Email/                       # MailKit implementation
│       │   │   ├── Storage/                     # Imágenes (local, S3-compatible)
│       │   │   └── Services/                    # DateTime provider, etc.
│       │   └── UserManagement.WebApi/           # Controllers, Middleware, Program.cs
│       │       ├── Controllers/
│       │       ├── Middleware/
│       │       ├── Filters/
│       │       └── Program.cs
│       └── tests/
│           ├── UserManagement.Domain.Tests/
│           ├── UserManagement.Application.Tests/
│           ├── UserManagement.Infrastructure.Tests/
│           └── UserManagement.WebApi.Tests/
├── frontend/
│   ├── src/
│   │   ├── components/          # UI components (shadcn/ui + custom)
│   │   ├── features/            # Módulos por funcionalidad
│   │   │   ├── auth/            # Login, Register, ForgotPassword
│   │   │   ├── users/           # CRUD usuarios
│   │   │   ├── roles/           # Gestión de roles
│   │   │   ├── profile/         # Perfil, foto, cambio clave
│   │   │   └── admin/           # Dashboard admin
│   │   ├── hooks/               # Custom hooks
│   │   ├── lib/                 # Utilidades, axios instance, helpers
│   │   ├── store/               # Estado global (Zustand o Context API)
│   │   ├── layouts/             # Navbar + Sidebar + Main layout
│   │   ├── pages/               # Páginas (React Router)
│   │   ├── types/               # TypeScript types/interfaces
│   │   └── App.tsx
│   └── tests/                   # Vitest + Testing Library
├── docker/
│   ├── Dockerfile.backend
│   ├── Dockerfile.frontend
│   └── nginx.conf
├── docker-compose.yml
├── docker-compose.override.yml  # Desarrollo
├── .gitignore
└── README.md
```

### Principios Arquitectónicos

- **Hexagonal**: Dominio en el centro, sin dependencias externas. Infraestructura implementa puertos definidos en Application.
- **CQRS tácito**: Separación de queries (lectura) y commands (escritura) usando MediatR.
- **Vertical Slicing**: Cada feature es autónoma (comando/query, handler, validator, mapper, DTOs).
- **SOLID**: Cada clase tiene una única responsabilidad.
- **Separación de preocupaciones**: WebApi solo conoce Application, no Infrastructure directamente.

## 3. Modelo de Datos (Code-First con EF Core)

### Entidades Principales

```
User
├── Id (Guid)
├── Email (string, unique, indexed)
├── PasswordHash (string)
├── SecurityStamp (string)           # Cambia al modificar clave/email
├── FirstName (string)
├── LastName (string)
├── AvatarPath (string?, nullable)   # Ruta de la imagen
├── EmailConfirmed (bool)
├── EmailConfirmationToken (string?)
├── EmailConfirmationTokenExpires (DateTime?)
├── TwoFactorEnabled (bool)          # Preparado para 2FA futuro
├── IsActive (bool)                  # Soft disable
├── LastLoginAt (DateTime?)
├── LastPasswordChangeAt (DateTime?)
├── PasswordExpirationDays (int)     # Días para expirar clave (default 30)
├── AccessFailedCount (int)          # Intentos fallidos
├── LockoutEnd (DateTime?)           # Bloqueo por intentos
├── LockoutEnabled (bool)
├── CreatedAt (DateTime)
├── CreatedBy (Guid?)                # Quién creó el usuario
├── UpdatedAt (DateTime?)
├── UpdatedBy (Guid?)
├── DeletedAt (DateTime?)            # Soft delete
├── ConcurrencyToken (byte[])        # Control de concurrencia
│
├── UserRoles (ICollection<UserRole>)
└── RefreshTokens (ICollection<RefreshToken>)

Role
├── Id (Guid)
├── Name (string, unique)
├── NormalizedName (string)
├── Description (string)
├── IsSystem (bool)                  # Roles de sistema no eliminables
├── Permissions (ICollection<RolePermission>)
├── CreatedAt (DateTime)
└── CreatedBy (Guid?)

RolePermission (relación muchos-a-muchos entre Role y Permission)
├── RoleId (Guid)
├── PermissionId (Guid)
└── Granted (bool)                   # true = concedido, false = denegado explícitamente

Permission (Catálogo de permisos)
├── Id (Guid)
├── Code (string, unique)            # Ej: "users.create", "users.delete"
├── Name (string)
├── Module (string)                  # Agrupación: "Users", "Roles", "Admin"
└── Description (string)

RefreshToken
├── Id (Guid)
├── UserId (Guid)
├── JwtId (Guid)
├── Token (string, hasheado)
├── DeviceInfo (string?)             # User-Agent / device fingerprint
├── IpAddress (string?)
├── CreatedAt (DateTime)
├── ExpiresAt (DateTime)
├── RevokedAt (DateTime?)            # Si fue revocado manualmente
├── RevokedBy (Guid?)                # Quién revocó
└── ReplacedByToken (string?)        # Rotación de tokens

AuditLog
├── Id (Guid)
├── UserId (Guid?)
├── Action (string)                  # "UserCreated", "UserLoggedIn", "PasswordChanged"
├── EntityType (string)              # "User", "Role", "Permission"
├── EntityId (string?)
├── OldValues (JSON?)
├── NewValues (JSON?)
├── IpAddress (string)
├── UserAgent (string)
├── Timestamp (DateTime)
└── Details (string?)

LoginAttempt
├── Id (Guid)
├── Email (string)
├── IpAddress (string)
├── AttemptedAt (DateTime)
├── Success (bool)
└── FailureReason (string?)
```

### Índices Clave

```sql
-- User: Email (unique index)
-- User: NormalizedEmail (unique index)
-- RefreshToken: JwtId (unique index)
-- RefreshToken: UserId + RevokedAt (filtered index)
-- AuditLog: Timestamp (included columns)
-- AuditLog: UserId + Timestamp (included columns)
-- LoginAttempt: IpAddress + AttemptedAt (para rate limiting a nivel BD)
```

## 4. Seguridad (Enterprise-Grade)

### 4.1 Autenticación JWT

| Aspecto | Configuración |
|---------|--------------|
| Algoritmo | RS256 (asimétrico) o HS512 (simétrico) |
| Access Token TTL | 15 minutos (configurable) |
| Refresh Token TTL | 7 días (configurable) |
| Refresh Token Rotation | Sí — cada vez que se usa, se rota |
| Refresh Token Reuse Detection | Sí — detecta si un token reusado fue revocado |
| JWT ID (jti) | Guid único por token, registrado en BD |
| Almacenamiento Refresh Token | Hasheado en BD (SHA-256) |
| Sliding Session | No — sesión fija con renovación explícita |
| Token Revocation | Por jti individual, por usuario, o global |

### 4.2 Política de Contraseñas

| Regla | Valor | Configurable |
|-------|-------|-------------|
| Longitud mínima | 10 caracteres | Sí |
| Requerir mayúscula | Sí | Sí |
| Requerir minúscula | Sí | Sí |
| Requerir dígito | Sí | Sí |
| Requerir carácter especial | Sí | Sí |
| Expiración (días) | 30 | Sí |
| Historial de claves | Últimas 5 | Sí |
| Intentos fallidos antes de bloqueo | 5 | Sí |
| Duración del bloqueo | 15 minutos | Sí |

### 4.3 Rate Limiting

```json
{
  "RateLimiting": {
    "Login": {
      "Window": "00:01:00",          // 1 minuto
      "MaxRequests": 10,
      "QueueLimit": 0
    },
    "ForgotPassword": {
      "Window": "01:00:00",          // 1 hora
      "MaxRequests": 3,
      "QueueLimit": 0
    },
    "Register": {
      "Window": "01:00:00",          // 1 hora
      "MaxRequests": 5,
      "QueueLimit": 0
    },
    "GlobalApi": {
      "Window": "00:01:00",
      "MaxRequests": 100,
      "QueueLimit": 2
    }
  }
}
```

### 4.4 Cloudflare Turnstile (reCAPTCHA alternativo)

- **Condicional**: Se verifica si `Captcha:SiteKey` y `Captcha:SecretKey` están configurados en appsettings.
- **Fallback transparente**: Si no hay keys configuradas, se omite la verificación.
- **Uso**: Solo en formulario de login y "olvidé mi contraseña".

### 4.5 Seguridad en Capas

```
Cliente (Frontend)
├── HTTPS obligatorio
├── Content Security Policy (CSP) headers
├── X-Frame-Options: DENY
├── X-Content-Type-Options: nosniff
├── Referrer-Policy: strict-origin-when-cross-origin
├── Permissions-Policy (cámara, micrófono solo cuando se requiera)
├── CSRF Token en formularios críticos
└── Sanitización de inputs

Cloudflare (CDN/WAF)
├── WAF rules (protección OWASP Top 10)
├── DDoS protection
├── SSL/TLS (Full Strict)
├── Bot Fight Mode
└── Turnstile (verificación captcha)

Backend (API)
├── CORS restringido al dominio frontend
├── Rate Limiting por IP y por usuario
├── Anti-forgery tokens (CSRF)
├── Validación de inputs (FluentValidation)
├── SQL Injection prevention (EF Core parameterized queries)
├── Security headers (NWebsec middleware)
├── Password hashing (PBKDF2 / bcrypt / Argon2id)
├── JWT signing + validation estricta
├── Refresh Token rotation + reuse detection
├── IP whitelist para endpoints admin (opcional)
├── Logging de eventos de seguridad
└── Filtro de sanitización de respuestas (no exponer datos sensibles)

Base de Datos
├── Conexión SSL/TLS
├── Usuario dedicado con mínimos privilegios
├── Encriptación en reposo (TDE o similar de PostgreSQL)
├── Backups automáticos cifrados
└── Connection string en secrets manager (no en código)
```

### 4.6 Manejo de Sesiones

- **30 segundos antes de expirar**: El frontend dispara un modal con "Su sesión está por expirar. ¿Desea continuar?".
- Si el usuario confirma: se llama al endpoint `/auth/refresh` con el refresh token.
- Si no confirma o expira: se limpia el store y se redirige al login.
- **Detección de sesión concurrente** (opcional): Si un usuario inicia sesión desde otro dispositivo, se puede optar por invalidar la sesión anterior o permitir sesiones múltiples (configurable).

### 4.7 Auditoría Completa (AuditLog)

Todas las operaciones críticas se registran:

| Evento | Detalle |
|--------|---------|
| Login exitoso | Email, IP, User-Agent, timestamp |
| Login fallido | Email, IP, razón, timestamp |
| Logout | UserId, IP |
| Creación de usuario | Quién creó, qué datos, IP |
| Modificación de usuario | Quién modificó, valores antiguos/nuevos |
| Eliminación de usuario (soft) | Quién eliminó, timestamp |
| Cambio de contraseña | UserId, IP |
| Reseteo de contraseña (admin) | Quién reseteó, a quién |
| Revocación de tokens | Quién revocó, alcance (usuario/todos) |
| Asignación/remoción de roles | Quién, a quién, qué rol |
| Cambio de permisos | Quién, qué permiso, conceder/denegar |
| Bloqueo/desbloqueo de usuario | Quién, a quién |

## 5. Envío de Correos Electrónicos

### 5.1 Configuración SMTP (Gmail)

```json
{
  "Email": {
    "Provider": "Smtp",
    "Smtp": {
      "Host": "smtp.gmail.com",
      "Port": 587,
      "EnableSsl": true,
      "Username": "",
      "Password": ""  // App password de Gmail
    },
    "FromName": "Sistema Gestión Usuarios",
    "FromEmail": "noreply@example.com",
    "Templates": {
      "Welcome": {
        "Subject": "Bienvenido al Sistema de Gestión de Usuarios",
        "TemplateFile": "welcome.html"
      },
      "EmailConfirmation": {
        "Subject": "Confirma tu correo electrónico",
        "TemplateFile": "email-confirmation.html"
      },
      "PasswordReset": {
        "Subject": "Restablecimiento de contraseña",
        "TemplateFile": "password-reset.html"
      },
      "PasswordChanged": {
        "Subject": "Tu contraseña ha sido cambiada",
        "TemplateFile": "password-changed.html"
      },
      "TemporaryPassword": {
        "Subject": "Contraseña temporal de acceso",
        "TemplateFile": "temporary-password.html"
      },
      "AccountLocked": {
        "Subject": "Tu cuenta ha sido bloqueada",
        "TemplateFile": "account-locked.html"
      }
    },
    "RetryCount": 3,
    "RetryDelaySeconds": 5,
    "QueueEnabled": true  // Usa Quartz.NET para encolar correos
  }
}
```

### 5.2 Plantillas de Correo

- Templates HTML con diseño responsivo.
- Almacenadas en `Infrastructure/Email/Templates/`.
- Usan variables como `{{UserName}}`, `{{ConfirmationLink}}`, `{{TempPassword}}`.
- Incluyen branding (logo, colores corporativos).

## 6. Gestión de Sesiones y Tokens

### 6.1 Flujo de Tokens

```
1. Login exitoso → Se genera:
   - Access Token (15 min) + Refresh Token (7 días)
   - Se registra RefreshToken en BD con jti único
   
2. Cada solicitud autenticada:
   - Middleware valida Access Token (firma, expiración, jti no revocado)
   
3. Refrescar token:
   - POST /auth/refresh con Refresh Token actual
   - Se valida que el RT exista, no esté revocado, no haya expirado
   - Se revoca el RT anterior (rotación)
   - Se emiten nuevos Access + Refresh Token
   - Si el RT anterior ya estaba revocado → posible robo → revocar todos los RT del usuario
   
4. Logout:
   - Se revoca el Refresh Token activo del usuario
   
5. Revocación masiva (admin):
   - Revocar por usuario específico
   - Revocar todos los tokens de todos los usuarios
```

### 6.2 Detección de Robo de Refresh Token

- Si un Refresh Token ya revocado es reutilizado, se asume compromiso.
- Se revocan **todos** los Refresh Tokens del usuario afectado.
- Se notifica al usuario por correo.
- Se registra en AuditLog.

## 7. Roles y Permisos (RBAC)

### Roles Iniciales

| Rol | Descripción |
|-----|-------------|
| `SuperAdmin` | Acceso total al sistema. No se puede eliminar. Creado automáticamente al seed. |
| `Admin` | Gestión de usuarios, roles, permisos. Crea y asigna roles del sistema. |
| `user-tipo-a` | Acceso a la página/sección A del sistema. Rol de ejemplo. |
| `user-tipo-b` | Acceso a la página/sección B del sistema. Rol de ejemplo. |
| `user-tipo-c` | Acceso a la página/sección C del sistema. Rol de ejemplo. |

**Nota**: Estos roles son ejemplos dinámicos — se pueden crear, editar y eliminar desde la grilla de administración de roles en el frontend. El sistema está diseñado para que en producción se agreguen los roles reales según las necesidades del negocio.

### Permisos (granulares)

```
users:list
users:create
users:edit
users:delete
users:activate
users:reset-password
users:revoke-tokens
users:view-sensitive      # Ver email, último login, etc.
roles:list
roles:create
roles:edit
roles:delete
roles:assign
permissions:list
permissions:assign
audit:view
admin:dashboard
admin:settings
```

### Páginas por Rol (Ejemplo MVP)

| Rol | Páginas/Secciones |
|-----|------------------|
| `SuperAdmin` | Todas (dashboard, usuarios, roles, permisos, auditoría, configuración) |
| `Admin` | Dashboard admin, gestión de usuarios, roles, permisos, auditoría |
| `user-tipo-a` | Página/sección A (ej: dashboard de reporting) |
| `user-tipo-b` | Página/sección B (ej: panel de operaciones) |
| `user-tipo-c` | Página/sección C (ej: portal de autoservicio) |

Cada usuario puede tener **múltiples roles**, heredando los permisos y accesos de todos ellos.

### Permisos por Defecto

- **SuperAdmin**: Todos los permisos
- **Admin**: `users:*`, `roles:*`, `permissions:*`, `audit:view`, `admin:dashboard`
- **user-tipo-a**: `page-a:view`, `profile:own:*`
- **user-tipo-b**: `page-b:view`, `profile:own:*`
- **user-tipo-c**: `page-c:view`, `profile:own:*`

## 8. Características del Frontend

### 8.1 Diseño UI/UX

- **Framework**: React 19 + TypeScript + Vite
- **CSS**: Tailwind CSS 4 + shadcn/ui (Radix primitives)
- **Tema**: Modo claro/oscuro con persistencia (localStorage + clase `dark` en `<html>`)
- **Responsive**: Mobile-first, sidebar colapsable, navbar adaptativa
- **Accesibilidad**: Roles ARIA, focus management, navegación por teclado, contraste suficiente
- **Notificaciones**: Sistema de toasts (sonner o similar)
- **Confirmaciones**: Modales de confirmación para acciones destructivas

### 8.2 Páginas y Componentes

#### Layout Principal
```
┌─────────────────────────────────────┐
│ Navbar                              │
│ ┌─ Logo ─┤ Búsqueda │── Usuario ─┐ │
├──────┬──────────────────────────────┤
│      │                              │
│ Side │     Contenido Principal      │
│ bar  │                              │
│      │                              │
│      │                              │
└──────┴──────────────────────────────┘
```

#### Módulo de Autenticación
- **Login**: Email + Password + Turnstile (si configurado)
- **Olvidé mi contraseña**: Email → envío de clave temporal
- **Cambio de contraseña obligatorio**: Al primer inicio con clave temporal o clave expirada
- **Sesión próxima a expirar**: Modal con countdown de 30 segundos

#### Gestión de Usuarios (Admin)
Tabla con:
- Paginación servidor-side (offset/limit)
- Ordenamiento por cualquier columna
- Búsqueda por email, nombre, rol
- Filtros: activo/inactivo, rol, fecha creación
- Acciones por fila: Editar, Eliminar (soft), Activar/Desactivar, Resetear clave
- Modal de creación/edición con todos los campos
- Carga de foto (drag & drop + webcam)
- Selector de roles (multi-select)

#### Perfil de Usuario
- Ver/editar nombre
- Cambiar foto (subir archivo + webcam)
- Cambiar contraseña (con verificación de clave actual)
- Historial de actividad reciente

#### Dashboard Admin
- Cards con métricas: total usuarios, nuevos (últimos 7 días), activos, bloqueados
- Tabla de últimas actividades (audit log)
- Cuentas por vencer (claves próximas a expirar)

#### Roles y Permisos
- CRUD de roles
- Asignación de permisos por rol (checkboxes agrupados por módulo)
- Vista de usuarios por rol

#### Gestión de Sesiones
- Listado de tokens activos por usuario
- Opción para revocar por usuario o global
- Botón "Cerrar todas las sesiones excepto la mía"

### 8.3 Manejo de Estado (Frontend)

```
Estado Global (Zustand / Context)
├── authStore
│   ├── user: User | null
│   ├── accessToken: string | null
│   ├── refreshToken: string | null
│   ├── permissions: string[]
│   ├── isAuthenticated: boolean
│   ├── sessionExpiringAt: number (timestamp)
│   ├── login(email, password)
│   ├── logout()
│   ├── refreshSession()
│   └── checkPermission(permissionCode)
│
├── uiStore
│   ├── sidebarOpen: boolean
│   ├── theme: 'light' | 'dark'
│   └── toasts: Toast[]
│
└── adminStore (opcional, o usar React Query)
    ├── users: User[]
    ├── roles: Role[]
    └── auditLogs: AuditEntry[]
```

### 8.4 Webcam para Foto de Perfil

- Usar API `navigator.mediaDevices.getUserMedia`
- Componente con preview en vivo
- Botón "Tomar foto" → captura del canvas
- Confirmación antes de guardar
- Fallback a subida de archivo si la webcam no está disponible
- Compresión y redimensionamiento antes de enviar al servidor

## 9. API Endpoints

### Autenticación
```
POST   /api/auth/login                    # Login
POST   /api/auth/refresh                  # Refresh token
POST   /api/auth/logout                   # Logout
POST   /api/auth/forgot-password          # Solicitar restablecimiento
POST   /api/auth/reset-password           # Resetear con token temporal
POST   /api/auth/change-password          # Cambiar clave (autenticado)
POST   /api/auth/confirm-email            # Confirmar correo
POST   /api/auth/verify-turnstile         # Verificar token Turnstile (opcional)
```

### Usuarios
```
GET    /api/users                         # Listar (paginado, ordenable, filtrable)
GET    /api/users/{id}                    # Obtener detalle
POST   /api/users                         # Crear usuario (admin)
PUT    /api/users/{id}                    # Actualizar usuario (admin)
DELETE /api/users/{id}                    # Soft delete
PATCH  /api/users/{id}/activate           # Activar/desactivar
PATCH  /api/users/{id}/reset-password     # Resetear clave (admin)
PATCH  /api/users/{id}/revoke-tokens      # Revocar tokens del usuario
POST   /api/users/{id}/avatar             # Subir/capturar foto
GET    /api/users/{id}/avatar             # Obtener foto

GET    /api/users/export                  # Exportar listado a CSV/Excel
POST   /api/users/import                  # Importar usuarios desde CSV
```

### Perfil Propio
```
GET    /api/profile                       # Mi perfil
PUT    /api/profile                       # Actualizar mi perfil
PUT    /api/profile/avatar                # Mi foto
GET    /api/profile/activity              # Mi actividad reciente
```

### Roles
```
GET    /api/roles                         # Listar roles
GET    /api/roles/{id}                    # Detalle de rol (con permisos)
POST   /api/roles                         # Crear rol
PUT    /api/roles/{id}                    # Actualizar rol
DELETE /api/roles/{id}                    # Eliminar (solo si no es sistema)
GET    /api/roles/{id}/users              # Usuarios con este rol
PATCH  /api/roles/{id}/permissions        # Actualizar permisos del rol
```

### Permisos
```
GET    /api/permissions                   # Listar todos los permisos
GET    /api/permissions/modules           # Permisos agrupados por módulo
```

### Administración
```
GET    /api/admin/dashboard               # Métricas del dashboard
GET    /api/admin/audit-log               # Log de auditoría (paginado, filtrable)
POST   /api/admin/revoke-all-tokens       # Revocar todos los tokens de sesión
GET    /api/admin/health                  # Estado del sistema
GET    /api/admin/metrics                 # Métricas avanzadas
```

### Health Checks
```
GET    /health                            # Health check básico
GET    /health/ready                      # Readiness (BD, Redis si aplica)
GET    /health/live                       # Liveness
```

## 10. Middleware y Pipeline de Backend

### Pipeline de Middleware (orden)

```
1. Exception Handling Middleware
2. HTTPS Redirection
3. HSTS (en producción)
4. Security Headers (X-Frame-Options, CSP, etc.)
5. CORS
6. Static Files (solo si sirve frontend)
7. Rate Limiting
8. Request Logging (Serilog)
9. Authentication
10. Authorization
11. Anti-Forgery (CSRF) — opcional si se usa JWT en header
12. Audit Logging (registro de operaciones)
13. Endpoints (Controllers)
```

### Filtros Globales

- `ModelValidationFilter`: Valida automáticamente los modelos con FluentValidation
- `AuditFilter`: Registra automáticamente operaciones de escritura
- `PerformanceFilter`: Mide tiempo de respuesta de endpoints

## 11. Configuraciones Externalizadas

```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=...;Database=...;Username=...;Password=..."
  },
  "Jwt": {
    "SecretKey": "",                    // Mínimo 64 chars para HS512
    "Issuer": "https://mvp-usuarios-back.example.com",
    "Audience": "https://mvp-usuarios-front.example.com",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7,
    "ClockSkewSeconds": 60
  },
  "PasswordPolicy": {
    "RequiredLength": 10,
    "RequiredUniqueChars": 4,
    "RequireNonAlphanumeric": true,
    "RequireLowercase": true,
    "RequireUppercase": true,
    "RequireDigit": true,
    "ExpirationDays": 30,
    "PasswordHistoryCount": 5,
    "MaxFailedAccessAttempts": 5,
    "DefaultLockoutMinutes": 15
  },
  "Captcha": {
    "Provider": "Cloudflare",           // "Cloudflare" | "None"
    "SiteKey": "",
    "SecretKey": ""
  },
  "RateLimiting": { /* ... */ },
  "Email": { /* ... */ },
  "Logging": {
    "Serilog": {
      "MinimumLevel": {
        "Default": "Information",
        "Override": {
          "Microsoft.AspNetCore": "Warning",
          "Microsoft.EntityFrameworkCore": "Warning"
        }
      },
      "WriteTo": [
        { "Name": "Console" },
        { "Name": "File", "Args": { "path": "logs/log-.txt", "rollingInterval": "Day" } }
      ]
    }
  },
  "Cors": {
    "AllowedOrigins": ["https://mvp-usuarios-front.example.com", "http://localhost:5173"],
    "AllowedMethods": ["GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"],
    "AllowedHeaders": ["Authorization", "Content-Type", "X-CSRF-TOKEN"]
  },
  "Storage": {
    "Provider": "Local",                 // "Local" | "S3"
    "BasePath": "/app/storage/avatars",
    "MaxFileSize": 5242880,              // 5 MB
    "AllowedExtensions": [".jpg", ".jpeg", ".png", ".webp"]
  },
  "Session": {
    "ExpirationWarningSeconds": 30,      // Aviso antes de expirar
    "AllowConcurrentSessions": true
  }
}
```

**Variable de Entorno**: Todas las configuraciones sensibles se deben poder sobreescribir con variables de entorno. Ej: `Jwt__SecretKey`, `ConnectionStrings__PostgreSQL`.

## 12. Despliegue (Docker + Traefik)

### Estructura docker-compose.yml

```yaml
version: "3.8"

services:
  traefik:
    image: traefik:v3.1
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
      - ./traefik/traefik.yml:/traefik.yml
      - ./traefik/dynamic.yml:/dynamic.yml
    labels:
      - "traefik.http.routers.dashboard.rule=Host(`traefik.example.com`)"
    networks:
      - web

  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: user_management
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER} -d user_management"]
      interval: 10s
      timeout: 5s
      retries: 5
    networks:
      - internal
    restart: unless-stopped

  backend:
    build:
      context: ./backend
      dockerfile: ../docker/Dockerfile.backend
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__PostgreSQL: "Host=postgres;Database=user_management;Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
      Jwt__SecretKey: ${JWT_SECRET_KEY}
      Captcha__SecretKey: ${CAPTCHA_SECRET_KEY}
      Captcha__SiteKey: ${CAPTCHA_SITE_KEY}
      Email__Smtp__Username: ${SMTP_USERNAME}
      Email__Smtp__Password: ${SMTP_PASSWORD}
    depends_on:
      postgres:
        condition: service_healthy
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.backend.rule=Host(`mvp-usuarios-back.example.com`)"
      - "traefik.http.routers.backend.entrypoints=websecure"
      - "traefik.http.routers.backend.tls.certresolver=cloudflare"
      - "traefik.http.services.backend.loadbalancer.server.port=8080"
    networks:
      - web
      - internal
    restart: unless-stopped

  frontend:
    build:
      context: ./frontend
      dockerfile: ../docker/Dockerfile.frontend
    depends_on:
      - backend
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.frontend.rule=Host(`mvp-usuarios-front.example.com`)"
      - "traefik.http.routers.frontend.entrypoints=websecure"
      - "traefik.http.routers.frontend.tls.certresolver=cloudflare"
      - "traefik.http.services.frontend.loadbalancer.server.port=80"
    networks:
      - web
    restart: unless-stopped

  # Opcional: backup automático de BD
  pg-backup:
    image: prodrigestivill/postgres-backup-local:16-alpine
    environment:
      POSTGRES_HOST: postgres
      POSTGRES_DB: user_management
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      BACKUP_DIR: /backups
      BACKUP_KEEP_DAYS: 30
      SCHEDULE: "@daily"
    volumes:
      - pg_backups:/backups
    depends_on:
      - postgres
    networks:
      - internal

networks:
  web:
    external: true
  internal:
    driver: bridge

volumes:
  postgres_data:
  pg_backups:
  avatars:
```

### Dockerfile.backend (multi-stage)

```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish src/UserManagement.WebApi -c Release -o /publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "UserManagement.WebApi.dll"]
```

### Dockerfile.frontend (multi-stage)

```dockerfile
# Stage 1: Build
FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

# Stage 2: Nginx
FROM nginx:1.27-alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

### nginx.conf para frontend

```nginx
server {
    listen 80;
    root /usr/share/nginx/html;
    index index.html;

    # Seguridad
    add_header X-Frame-Options "DENY" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;
    add_header Content-Security-Policy "default-src 'self'; script-src 'self' https://challenges.cloudflare.com; frame-src https://challenges.cloudflare.com; img-src 'self' data: blob:; connect-src 'self' https://mvp-usuarios-back.example.com; style-src 'self' 'unsafe-inline';" always;
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains; preload" always;

    # SPA routing
    location / {
        try_files $uri $uri/ /index.html;
    }

    # Proxy API
    location /api/ {
        proxy_pass http://backend:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # Cache de assets
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|webp)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }
}
```

### CI/CD Recomendado

```yaml
# .github/workflows/deploy.yml (GitHub Actions)
name: Deploy

on:
  push:
    branches: [main]

jobs:
  test:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:16-alpine
        env:
          POSTGRES_DB: test_db
          POSTGRES_USER: test
          POSTGRES_PASSWORD: test
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x
      - name: Restore
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore
      - name: Test
        run: dotnet test --no-build --verbosity normal

  deploy:
    needs: test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Deploy to Server
        uses: appleboy/ssh-action@v1.0.3
        with:
          host: ${{ secrets.SERVER_HOST }}
          username: ${{ secrets.SERVER_USER }}
          key: ${{ secrets.SSH_PRIVATE_KEY }}
          script: |
            cd /opt/user-management
            git pull
            docker compose pull
            docker compose up -d --build
            docker system prune -f
```

## 13. Testing

### Estrategia de Pruebas

| Tipo | Herramienta | Cobertura |
|------|-------------|-----------|
| Unitarias | xUnit + Moq + FluentAssertions | Domain + Application (Handlers, Validators) |
| Integración | Testcontainers (PostgreSQL) | Repositorios, DbContext, Flujos completos |
| API (Integration) | WebApplicationFactory + Testcontainers | Todos los endpoints |
| Frontend | Vitest + Testing Library | Componentes, Hooks, Pages |
| E2E (opcional) | Playwright | Flujos críticos (login, CRUD usuarios) |

### Pruebas Unitarias Obligatorias (por feature)

```csharp
[Fact]
public async Task Login_WithValidCredentials_ReturnsTokens()
[Fact]
public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
[Fact]
public async Task Login_WithLockedAccount_ReturnsLocked()
[Fact]
public async Task Login_WithExpiredPassword_RequiresPasswordChange()
[Fact]
public async Task Refresh_WithValidToken_ReturnsNewTokens()
[Fact]
public async Task Refresh_WithRevokedToken_ThrowsSecurityException()
[Fact]
public async Task CreateUser_WithValidData_PersistsUser()
[Fact]
public async Task CreateUser_WithDuplicateEmail_ThrowsConflict()
[Fact]
public async Task ChangePassword_WithWrongOldPassword_Fails()
[Fact]
public async Task RevokeAllTokens_ForUser_InvalidatesAllSessions()
// ... y así para cada caso de uso
```

### Pruebas de Integración

```csharp
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<PostgresTestContainer> { }

[Collection("Database")]
public class UserRepositoryTests
{
    [Fact]
    public async Task GetPagedAsync_WithFilters_ReturnsCorrectResults()
    [Fact]
    public async Task GetPagedAsync_WithSorting_ReturnsInCorrectOrder()
    [Fact]
    public async Task GetPagedAsync_WithSearch_ReturnsMatchingUsers()
}
```

## 14. Consideraciones de Compliance y Buenas Prácticas

### GDPR / Ley de Datos Personales
- Consentimiento explícito al crear cuenta
- Derecho de acceso: endpoint para que el usuario descargue sus datos
- Derecho de olvido: soft delete + anonimización programada
- Política de retención de datos: logs de auditoría por 1 año, tokens por 90 días
- Base legal del tratamiento registrada en el sistema

### OWASP Top 10 (2021)
- **A01: Broken Access Control**: Validación de permisos en cada endpoint
- **A02: Cryptographic Failures**: Passwords hasheados con Argon2id, JWT firmado
- **A03: Injection**: EF Core parameterized queries, validación de inputs
- **A04: Insecure Design**: Rate limiting, lockout, seguridad por diseño
- **A05: Security Misconfiguration**: Configuración por ambiente, secrets externalizados
- **A06: Vulnerable Components**: Dependencias auditadas con `dotnet list package --vulnerable`
- **A07: Identification and Authentication Failures**: JWT + refresh rotation + MFA-ready
- **A08: Software and Data Integrity Failures**: Verificación de paquetes, firmas
- **A09: Security Logging and Monitoring**: Serilog + audit log + health checks
- **A10: Server-Side Request Forgery**: No se hacen requests a URLs no controladas

### Logging y Monitoreo

```
Serilog Sinks:
├── Console (desarrollo)
├── File (rolling daily, producción)
├── PostgreSQL (opcional, para consultas de auditoría)
└── Prometheus (opcional, métricas)

Health Checks:
├── GET /health          → Liveness (responde si el proceso está vivo)
├── GET /health/ready    → Readiness (BD conectada, dependencias ready)
├── GET /health/startup  → Startup (aplicación lista para recibir tráfico)
└── GET /api/admin/health → Dashboard con detalle de todos los checks

Alertas:
├── Failed login rate > umbral
├── Errores 500 > umbral
├── BD desconectada
├── Certificado SSL próximo a vencer
└── Backup fallido
```

## 15. Secretos y Gestión de Configuración

### En Desarrollo
- `appsettings.Development.json` con valores locales
- `dotnet user-secrets` para credenciales locales

### En Producción
- **NUNCA** almacenar secrets en `appsettings.Production.json` en el repositorio.
- Usar variables de entorno en el docker-compose.
- Opcional: HashiCorp Vault, Azure Key Vault, o Docker Secrets.
- Ejemplo de `.env` (excluido del repositorio via `.gitignore`):

```bash
POSTGRES_USER=useradmin
POSTGRES_PASSWORD=changethis!
JWT_SECRET_KEY=supersecretkeywith64charactersminimum...
CAPTCHA_SITE_KEY=0x4AAAA...
CAPTCHA_SECRET_KEY=0x4AAAA...
SMTP_USERNAME=your-email@gmail.com
SMTP_PASSWORD=your-app-password
```

## 16. Roadmap de Implementación

### Fase 1: Base (Días 1-3)
1. Crear solución .NET con arquitectura hexagonal
2. Configurar EF Core + PostgreSQL + migración inicial
3. Implementar Domain entities + DbContext
4. Configurar Serilog + health checks
5. Dockerizar backend + frontend + docker-compose

### Fase 2: Autenticación (Días 4-6)
1. Implementar JWT (access + refresh token)
2. Endpoints de login, refresh, logout
3. Rate limiting + Turnstile condicional
4. Refresh token rotation + reuse detection
5. Frontend: login page, auth store, interceptors

### Fase 3: Gestión de Usuarios (Días 7-9)
1. CRUD de usuarios (backend + frontend)
2. Paginación, ordenamiento, búsqueda server-side
3. Roles y permisos (backend + frontend)
4. Avatar upload + webcam capture
5. Confirmación de email

### Fase 4: Seguridad Avanzada (Días 10-12)
1. Política de contraseñas + expiración
2. Forgot password flow
3. Cambio de contraseña obligatorio
4. Revocación de tokens (individual/masiva)
5. Audit logging completo

### Fase 5: Frontend Completo (Días 13-15)
1. Layout (navbar + sidebar + responsive)
2. Gestión de roles y permisos (UI)
3. Dashboard admin con métricas
4. Session expiration warning modal
5. Perfil de usuario + cambio de foto

### Fase 6: Calidad y Despliegue (Días 16-18)
1. Tests unitarios e integración
2. Configuración de CI/CD (GitHub Actions)
3. Despliegue en servidor con Traefik
4. Pruebas de seguridad (OWASP)
5. Documentación (README.md + API docs)
6. Configuración de backups

### Fase 7: Post-MVP (Opcional)
- 2FA / MFA (TOTP con Authenticator App)
- LDAP / SSO (SAML / OIDC para integración corporativa)
- Exportar reportes (PDF, Excel)
- WebSockets para notificaciones en tiempo real
- Internacionalización (i18n)
- API Key management para integraciones externas
- Dashboard de métricas con Prometheus + Grafana

## 17. Archivos y Configuración Inicial del Proyecto

### .gitignore
```
# .NET
bin/
obj/
*.user
*.suo
*.cache
*.tmp
.vs/
*.DotSettings

# Node
node_modules/
dist/
.env
.env.local

# Docker
docker-compose.override.yml

# Logs
logs/
*.log

# Storage
storage/

# IDE
.idea/
*.swp
*.swo
```

### README.md (estructura sugerida)
1. Descripción del proyecto
2. Stack tecnológico
3. Requisitos previos (Docker, .NET 10 SDK, Node.js 22)
4. Inicio rápido (local con docker compose)
5. Desarrollo local (backend + frontend por separado)
6. Migraciones de base de datos
7. Variables de entorno requeridas
8. Despliegue en producción
9. Estructura del proyecto
10. API Reference (link a Scalar UI)
11. Testing

## 18. Metodología de Desarrollo (OpenSpec)

Cada funcionalidad seguirá el flujo OpenSpec:

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│  Explore     │────▶│   Propose    │────▶│    Apply     │────▶│   Archive    │
│              │     │              │     │              │     │              │
│ Pensar,      │     │ proposal.md  │     │ Implementar  │     │ Archivar     │
│ cuestionar,  │     │ design.md    │     │ tareas del   │     │ cambio       │
│ visualizar   │     │ tasks.md     │     │ cambio       │     │ completado   │
└──────────────┘     └──────────────┘     └──────────────┘     └──────────────┘
```

### Flujo por Feature

1. **Explore** (`/opsx-explore`): Investigar requerimientos, revisar código existente, clarificar dudas.
2. **Propose** (`/opsx-propose`): Crear artefactos formales:
   - `proposal.md` — qué se va a hacer y por qué
   - `design.md` — cómo se va a implementar (diagramas, decisiones técnicas)
   - `tasks.md` — lista de tareas concretas para implementar
3. **Apply** (`/opsx-apply`): Implementar las tareas una por una, con tests.
4. **Archive** (`/opsx-archive`): Finalizar y archivar el cambio.

### Artefactos Generados

```
.opencode/
└── changes/
    └── <feature-name>/
        ├── .openspec.yaml        # Metadatos del cambio
        ├── proposal.md           # Alcance y justificación
        ├── design.md             # Diseño técnico detallado
        ├── tasks.md              # Lista de tareas ([ ] / [x])
        └── specs/                # Especificaciones detalladas
            └── <feature>/spec.md
```

## 19. Resumen de Mejoras Respecto al Plan Original

| Aspecto | Plan Original | Plan Mejorado |
|---------|--------------|---------------|
| Arquitectura | Hexagonal básica | Hexagonal + Vertical Slicing + CQRS (MediatR) |
| Seguridad | JWT + refresh básico | Refresh rotation, reuse detection, revocación, audit log completo, rate limiting granular, lockout, expiración de clave |
| Roles | 3 roles fijos | RBAC con permisos granulares, roles dinámicos, catálogo de permisos |
| Modelo de datos | Solo usuarios | Users, Roles, Permissions, RefreshTokens, AuditLogs, LoginAttempts |
| Correos | Envío básico | Templates HTML, cola de correos (Quartz.NET), retry, múltiples plantillas |
| Frontend | Navbar + sidebar + 3 páginas | Layout completo, gestión de roles/permissions, dashboard, perfil, webcam, session warning, dark/light mode |
| Monitoreo | Solo logs | Health checks, métricas, auditoría completa, alertas |
| Testing | Unitarios | Unitarios + Integración (Testcontainers) + API + Frontend |
| Infraestructura | Docker compose | Docker compose + CI/CD + backups + SSL automatizado + secrets management |
| Compliance | No mencionado | OWASP Top 10, GDPR-ready, políticas de retención |
