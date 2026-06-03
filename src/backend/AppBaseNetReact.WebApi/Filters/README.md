# Filters — Filtros y DTOs de Respuesta

| Archivo | Propósito |
|---------|-----------|
| `ApiResponse.cs` | Clase genérica `ApiResponse<T>` con `Success`, `Message`, `Data`, `Errors`. Incluye `PagedResponse<T>` para respuestas paginadas |

## Estructura de respuesta

```json
{
  "success": true,
  "message": "Operation completed",
  "data": { ... },
  "errors": null
}
```

Todas las respuestas de la API usan `ApiResponse<T>` como wrapper.
