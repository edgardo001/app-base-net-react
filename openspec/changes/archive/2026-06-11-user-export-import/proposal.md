## Why

El plan original define endpoints de exportación (`GET /api/users/export` a CSV) e importación (`POST /api/users/import` desde CSV) de usuarios. Estas funcionalidades son necesarias para operaciones de administración masiva, migraciones y backup de datos de usuarios sin acceso directo a BD.

## What Changes

- **Export**: Endpoint `GET /api/users/export` que devuelva un CSV con la lista de usuarios (filtrable por los mismos criterios que GET /api/users)
- **Import**: Endpoint `POST /api/users/import` que reciba un CSV, lo valide, y cree los usuarios en lote con reporte de errores
- **Frontend**: Botón "Exportar" en la grilla de usuarios + modal de importación con drag & drop de CSV

## Capabilities

### New Capabilities
- `user-export`: Exportación de usuarios a CSV con los mismos filtros de listado
- `user-import`: Importación masiva de usuarios desde CSV con validación y reporte

### Modified Capabilities
Ninguna — es funcionalidad nueva no cubierta por specs existentes

## Impact

- **Backend**: Nuevo QueryHandler para export + CommandHandler para import; posible NuGet para CSV (CsvHelper); validación de archivos
- **Frontend**: Botón exportar + modal importar en página de usuarios
- **Tests**: Tests para handlers de export e import
