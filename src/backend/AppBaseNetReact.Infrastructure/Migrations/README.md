# Migrations — Migraciones de EF Core

Migraciones generadas automáticamente por Entity Framework Core.

```bash
# Crear una nueva migración
dotnet ef migrations add <Nombre> -p src/backend/AppBaseNetReact.Infrastructure -s src/backend/AppBaseNetReact.WebApi

# Aplicar migraciones
dotnet ef database update -p src/backend/AppBaseNetReact.Infrastructure -s src/backend/AppBaseNetReact.WebApi
```

> No modificar manualmente — solo regenerar con `dotnet ef migrations add`.
