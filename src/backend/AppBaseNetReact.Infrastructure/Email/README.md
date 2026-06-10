# Email — Servicio de Correo Electrónico

Implementación del envío de correos transaccionales.

| Carpeta | Propósito |
|---------|-----------|
| `Templates/` | Plantillas HTML de correos (welcome, password-reset, email-confirmation, account-locked, etc.) |

Dependencias: `IEmailService` definido en `Application/Common/Interfaces/`, `EmailRenderer` para templates embebidos.
