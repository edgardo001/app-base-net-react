# AppBaseNetReact.Application — Casos de Uso

## Propósito

Capa de aplicación donde se definen los **puertos** (interfaces) y reside la **orquestación de la lógica de negocio**. Contiene los casos de uso de la aplicación siguiendo CQRS (estructuralmente preparado).

## Dependencias

- **Referencia:** `Domain`
- **Referenciado por:** `Infrastructure` (implementa sus interfaces), `WebApi` (usa sus handlers)
- **Paquetes NuGet:** MediatR, FluentValidation, AutoMapper

## Estructura

```
Common/
  Interfaces/
    IRepository.cs            — Puerto genérico: IRepository<T> con CRUD + paginación
    IRepositories.cs          — Puertos específicos: IUserRepository, IRoleRepository,
                                IRefreshTokenRepository, IAuditLogRepository,
                                IPermissionRepository, ILoginAttemptRepository
    IServices.cs              — Puertos de servicios: IJwtService, IPasswordHasherService,
                                IEmailService, ICaptchaService, IDateTimeProvider
    IAuditService.cs          — Puerto de auditoría
    IPasswordPolicyService.cs — Puerto de política de contraseñas
  Behaviors/
    ValidationBehavior.cs     — Pipeline de MediatR para FluentValidation
  Validators/
    AuthValidators.cs, ProfileValidators.cs, RoleValidators.cs, etc.
Features/
  Auth/     — Commands/ + Queries/ (pendiente de implementar) + Validators
  Users/    — Commands/ + Queries/ (pendiente)
  Roles/    — Commands/ + Queries/ (pendiente)
  Permissions/
  Profile/
  Dashboard/
  Audit/
DependencyInjection.cs        — RegisterServices(): AddMediatR, AddFluentValidation, AddAutoMapper
```

## Dónde ocurre la acción

| Estado | Responsable | Ubicación actual |
|--------|------------|------------------|
| ⚡ Actual | Controllers (vía IUnitOfWork) | `WebApi/Controllers/*` |
| 🎯 Target | Handlers CQRS | `Features/*/Commands|Queries/*Handler.cs` (a implementar) |

> 🔌 **Puertos definidos aquí**: Las interfaces en `Common/Interfaces/` son contratos que `Infrastructure` implementa. La capa de aplicación nunca conoce las implementaciones concretas.
