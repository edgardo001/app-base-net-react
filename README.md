# User Management Platform

Plataforma de gestión de usuarios con autenticación JWT, RBAC, y despliegue Docker con Traefik.

## Stack

| Capa | Tecnología |
|------|-----------|
| Backend | .NET 10, C# 13, ASP.NET Core Minimal + Controllers |
| Arquitectura | Hexagonal (Domain/Application/Infrastructure/WebApi) |
| ORM | Entity Framework Core 10, PostgreSQL 18 |
| Autenticación | JWT (HS512) con refresh token rotation + reuse detection |
| Caching | Passwords: PBKDF2 (Rfc2898DeriveBytes, 100k iteraciones) |
| Frontend | React 19, Vite 8, TypeScript, Tailwind CSS v4, shadcn/ui v4 |
| Estado | Zustand |
| Proxy | Traefik v3 con Let's Encrypt |
| Contenedores | Docker Compose |

## Requisitos

- .NET 10 SDK
- Node.js 22+
- Docker + Docker Compose
- PostgreSQL 18 (o usar el contenedor del docker-compose)

## Inicio rápido (desarrollo local)

```bash
# 1. Clonar y configurar variables de entorno
cp .env.template .env
# Editar .env con valores reales
# La clave JWT debe tener al menos 64 caracteres

# 2. Iniciar PostgreSQL (opcional, si no tienes local)
docker compose -f src/docker/docker-compose.yml up postgres -d

# 3. Backend
dotnet build UserManagement.slnx
dotnet run --project src/backend/UserManagement.WebApi

# 4. Frontend
cd src/frontend
npm install
npm run dev
```

## Estructura del proyecto

```
├── UserManagement.slnx          # Solución .NET
├── .env.template                # Template de variables de entorno
├── src/
│   ├── backend/
│   │   ├── UserManagement.Domain/       # Entidades, Value Objects, Enums
│   │   ├── UserManagement.Application/  # CQRS, Interfaces, Validación
│   │   ├── UserManagement.Infrastructure/ # EF Core, JWT, Email, Persistencia
│   │   └── UserManagement.WebApi/       # Controllers, Middleware, Program.cs
│   ├── frontend/
│   │   ├── src/stores/                  # Zustand (auth-store)
│   │   ├── src/lib/                     # API client, utils
│   │   ├── src/components/              # shadcn/ui, layout, auth
│   │   └── src/pages/                   # Login, Dashboard, Users, Roles...
│   └── docker/                          # Dockerfiles, nginx, docker-compose
├── AGENTS.md                    # Guía para asistentes IA
└── DESIGN.md                    # Documentación de arquitectura
```

## Variables de entorno

| Variable | Descripción |
|----------|-------------|
| `ConnectionStrings__PostgreSQL` | Cadena de conexión PostgreSQL |
| `Jwt__SecretKey` | Clave JWT (mínimo 64 caracteres para HS512) |
| `Jwt__Issuer` | Emisor del token |
| `Jwt__Audience` | Audiencia del token |
| `Captcha__SiteKey` | Cloudflare Turnstile Site Key |
| `Captcha__SecretKey` | Cloudflare Turnstile Secret Key |
| `Email__Smtp__Username` | Usuario SMTP |
| `Email__Smtp__Password` | Contraseña SMTP |

## Comandos principales

```bash
# Backend
dotnet build UserManagement.slnx
dotnet watch run --project src/backend/UserManagement.WebApi

# Frontend
cd src/frontend && npm run dev
npm run build              # Producción

# Base de datos (EF Core)
dotnet ef migrations add <Nombre> --project src/backend/UserManagement.Infrastructure --startup-project src/backend/UserManagement.WebApi
dotnet ef database update --project src/backend/UserManagement.Infrastructure --startup-project src/backend/UserManagement.WebApi

# Docker (full stack con Traefik)
docker compose -f src/docker/docker-compose.yml --env-file .env up -d
```

## Despliegue

El archivo `src/docker/docker-compose.yml` levanta:
1. **Traefik** — Proxy reverso con TLS automático (Let's Encrypt)
2. **PostgreSQL 18** — Base de datos
3. **Backend** — .NET 10 Web API
4. **Frontend** — React SPA servido por Nginx

Dominios por defecto (configurables en `.env`):
- Backend: `mvp-usuarios-back.example.com`
- Frontend: `mvp-usuarios-front.example.com`

## Seed Data

Al iniciar por primera vez, el seeder crea:
- **22 permisos** cubriendo usuarios, roles, permisos, dashboard, admin, perfil
- **5 roles**: SuperAdmin, Admin, user-tipo-a, user-tipo-b, user-tipo-c
- **Usuario admin**: `admin` / `admin` (SuperAdmin)
