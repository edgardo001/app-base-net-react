# Common — Base de Entidades

Contiene `BaseEntity.cs`, la clase base abstracta de la que heredan todas las entidades del dominio.

## BaseEntity

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Id` | `Guid` | Identificador único (generado al crear) |
| `CreatedAt` | `DateTime` | Timestamp de creación (set automático) |
| `UpdatedAt` | `DateTime?` | Timestamp de última modificación |
| `DeletedAt` | `DateTime?` | Soft-delete (null = activo) |
| `ConcurrencyToken` | `byte[]` | Row version para concurrencia optimista |
