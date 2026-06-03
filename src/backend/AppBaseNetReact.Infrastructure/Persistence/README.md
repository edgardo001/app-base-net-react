# Persistence — Acceso a Datos (EF Core + PostgreSQL)

Capa de persistencia que implementa los puertos de repositorio usando Entity Framework Core 10 con PostgreSQL 18.

| Archivo/Carpeta | Propósito |
|----------------|-----------|
| `AppDbContext.cs` | DbContext con override de `SaveChangesAsync` para auditoría automática (UpdatedAt) + soft-delete global query filter |
| `AppDbContextFactory.cs` | Factory para comandos CLI de EF Core (`dotnet ef migrations`) |
| `Configurations/` | `IEntityTypeConfiguration<T>` para cada entidad |
| `Repositories/` | Implementaciones concretas de los puertos `IRepository<T>` |
| `Migrations/` | Migraciones generadas por EF Core |
