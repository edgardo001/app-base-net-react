# User Management Platform — Agent Guide

## Project Structure
- `.env.template` — Template for environment variables (copy to `.env`)
- `UserManagement.slnx` — Solution file at repo root
- `src/backend/` — .NET 10 solution with hexagonal architecture
  - `UserManagement.Domain/` — Entities, ValueObjects, Enums, Common
  - `UserManagement.Application/` — Commands, Queries, Validators, Interfaces
  - `UserManagement.Infrastructure/` — Persistence, Services, Identity, Email
  - `UserManagement.WebApi/` — Controllers, Middleware, Filters, Program.cs
- `src/frontend/` — React 19 + Vite + TypeScript + Tailwind CSS v4 + shadcn/ui v4
  - `src/stores/` — Zustand stores (auth-store)
  - `src/lib/` — API client, utility functions
  - `src/components/ui/` — shadcn/ui components (button, card, input, etc.)
  - `src/components/layout/` — Layout (sidebar, header)
  - `src/components/auth/` — Auth guards
  - `src/pages/` — Page components (login, dashboard, users, roles, etc.)
- `src/docker/` — Dockerfiles (backend, frontend), nginx.conf, docker-compose.yml
- `.opencode/` — OpenSpec skills and config

## Key Commands
```bash
# Build .NET backend
dotnet build UserManagement.slnx

# Run backend with watch
dotnet watch run --project src/backend/UserManagement.WebApi

# Build frontend
cd src/frontend && npm run build

# Run frontend dev
cd src/frontend && npm run dev

# Run all tests
dotnet test UserManagement.slnx

# Docker compose up (full stack + Traefik)
docker compose -f src/docker/docker-compose.yml --env-file .env up -d

# EF Migrations
dotnet ef migrations add <Name> --project src/backend/UserManagement.Infrastructure --startup-project src/backend/UserManagement.WebApi
dotnet ef database update --project src/backend/UserManagement.Infrastructure --startup-project src/backend/UserManagement.WebApi
```

## Architecture Rules
- Domain layer: ZERO dependencies on external packages
- Application layer: Only depends on Domain + NuGet packages (MediatR, FluentValidation, AutoMapper)
- Infrastructure layer: Implements interfaces from Application, depends on Domain
- WebApi layer: References Application AND Infrastructure (pragmatic hexagonal — Infra extension methods needed at startup)
- Controllers inject IUnitOfWork and services, not repositories directly
- Frontend uses Zustand for state, shadcn/ui + Tailwind v4 for styling, axios for HTTP

## Environment Variables (never hardcode)
| Variable | Description |
|----------|-------------|
| `ConnectionStrings__PostgreSQL` | Postgres connection string |
| `Jwt__SecretKey` | JWT signing key (min 64 chars) |
| `Jwt__Issuer` | Token issuer URL |
| `Jwt__Audience` | Token audience URL |
| `Captcha__SiteKey` | Cloudflare Turnstile site key |
| `Captcha__SecretKey` | Cloudflare Turnstile secret key |
| `Email__Smtp__Username` | SMTP username |
| `Email__Smtp__Password` | SMTP password |

## Conventions
- Use kebab-case for folders/files in frontend components
- Use PascalCase for C# classes and files
- All API responses use `ApiResponse<T>` wrapper
- Commands = CQRS write operations, Queries = read operations
- Each feature has: Command/Query → Handler → Validator → DTO/Response
- Tests mirror source structure: `tests/UserManagement.*.Tests/`

## OpenSpec Workflow
- `/opsx-explore` — Think through problems before coding
- `/opsx-propose` — Create change proposals with specs, design, tasks
- `/opsx-apply` — Implement tasks from a change
- `/opsx-archive` — Archive completed changes

## Database
- Provider: PostgreSQL 18
- Database name: `mvp-usuarios-db`
- User: `mvp-usuarios-db`
- Password: Random generated, stored in `.env` (gitignored)
- ORM: Entity Framework Core 10 Code-First
