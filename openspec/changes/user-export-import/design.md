## Context

La aplicación permite CRUD de usuarios via API pero no ofrece exportación a CSV ni importación masiva. Para operaciones administrativas (migraciones, backups, carga inicial) es necesario poder exportar usuarios con los mismos filtros de listado e importar usuarios desde un archivo CSV.

## Goals / Non-Goals

**Goals:**
- `GET /api/users/export` → descarga CSV con usuarios paginados/filtrados (mismos filtros que listado)
- `POST /api/users/import` → recibe CSV, valida, crea usuarios, retorna reporte de resultados
- Frontend: botón "Exportar" en la grilla + modal de importación con drag & drop
- Manejo de errores por fila en importación (reporte detallado)

**Non-Goals:**
- No se implementa export a Excel (solo CSV)
- No se implementa import desde JSON ni otros formatos
- No se implementa import con actualización de usuarios existentes (solo creación)
- No se implementa export programático (solo descarga HTTP)

## Decisions

### CsvHelper vs manual string building
- **Decisión**: Usar `CsvHelper` NuGet para export e import
- **Rationale**: Maneja escaping, encoding, mapeo por atributos, y validación de tipos. Evita bugs de CSV manual
- **Alternativa**: StringBuilder manual — más simple pero propenso a errores de escaping

### Import como lote con transacción vs fila por fila
- **Decisión**: Procesar fila por fila recolectando errores, sin transacción global. Las filas válidas se crean, las inválidas se reportan
- **Rationale**: Permite importaciones parciales. Una transacción global haría que un error en la fila 500 revierta las 499 anteriores
- **Alternativa**: Transacción por lote de 100 filas — mejoraría consistencia pero complica el reporte de errores

## Risks / Trade-offs

- [Risk] Carga de archivos grandes puede agotar memoria → Mitigación: límite de 10MB y 5000 filas max
- [Risk] CSV con encoding incorrecto (UTF-8 con BOM vs Latin1) → Mitigación: detectar BOM automáticamente, fallback a UTF-8
- [Trade-off] Sin upsert: usuarios con email existente se reportan como error, no se actualizan
