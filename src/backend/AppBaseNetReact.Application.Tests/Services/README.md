# Services — Tests de Validadores y Servicios

Pruebas unitarias para:
- **Validadores FluentValidation** — reglas de validación de Requests
- **Servicios de aplicación** — lógica de servicios (actualmente enfocado en validadores)

Ejemplos:
- `LoginRequestValidator_EmptyEmail_Fails`
- `LoginRequestValidator_ValidRequest_Passes`
- `CreateUserRequestValidator_PasswordTooShort_Fails`
