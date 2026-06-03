# Validators — FluentValidation

Reglas de validación para los DTOs de entrada (Requests/Commands).

| Archivo | Validadores |
|---------|-------------|
| `AuthValidators.cs` | `LoginRequestValidator`, `RefreshRequestValidator`, `ChangePasswordValidator`, `ForgotPasswordValidator`, `ResetPasswordValidator` |
| `ProfileValidators.cs` | `UpdateProfileRequestValidator`, `ChangeEmailRequestValidator` |
| `RolePermissionValidators.cs` | `AssignPermissionsRequestValidator` |
| `RoleValidators.cs` | `CreateRoleRequestValidator`, `UpdateRoleRequestValidator` |
| `UserValidators.cs` | `CreateUserRequestValidator`, `UpdateUserRequestValidator`, `ResetPasswordRequestValidator` |

## Convenciones

- Un validador por Request/Command
- Reglas encadenadas: `RuleFor(x => x.Email).NotEmpty().EmailAddress()`
- Mensajes en español descriptivos
