# Controllers — Puntos de Entrada de la API REST

Controladores que exponen los endpoints HTTP. Inyectan `IUnitOfWork` + servicios para orquestar la lógica.

| Controller | Endpoints | Auth |
|------------|-----------|------|
| `AuthController` | `POST login`, `POST refresh`, `POST change-password`, `POST forgot-password`, `POST reset-password`, `POST logout` | Mixto |
| `UsersController` | `GET /`, `GET /{id}`, `POST /`, `PUT /{id}`, `DELETE /{id}`, `POST /{id}/activate`, `POST /{id}/deactivate`, `POST /{id}/reset-password`, `POST /{id}/revoke-tokens` | JWT |
| `RolesController` | `GET /`, `GET /{id}`, `POST /`, `PUT /{id}`, `DELETE /{id}`, `PUT /{id}/permissions` | JWT |
| `PermissionsController` | `GET /`, `GET /modules` | JWT |
| `ProfileController` | `GET /`, `PUT /`, `GET /activity` | JWT |
| `AdminController` | `GET /dashboard/stats`, `GET /audit-log`, `POST /revoke-all-tokens` | JWT |

> ⚡ **Estado actual:** Los controllers orquestan la lógica directamente. Target: migrar a handlers CQRS donde los controllers solo llamen `MediatR.Send()`.
