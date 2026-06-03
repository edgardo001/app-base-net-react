# AppBaseNetReact.Application.Tests — Tests de la Capa de Aplicación

## Propósito

Pruebas unitarias para la capa `Application`. Cubren:

- **Servicios/Válidadores** — FluentValidation rules (`AuthValidators`, `RoleValidators`, etc.)
- **Comportamiento de dominio** — Reglas de negocio en entidades (`User`, `Role`, `RefreshToken`)
- **Puertos/Contratos** — Comportamiento esperado de las interfaces

## Dependencias

- **Referencia:** `Application`
- **Framework:** xUnit + Moq + FluentAssertions

## Estructura

```
Domain/          — Tests de entidades de dominio (User, Role, RefreshToken, Permission)
Services/        — Tests de validadores y servicios de aplicación
UnitTest1.cs     — Template inicial (reemplazar o eliminar)
```

## Convenciones

- Nomenclatura: `[Clase]_[Método]_[Escenario]_[ResultadoEsperado]`
- Mock de interfaces con Moq, asserts con FluentAssertions
- Los tests de validadores usan instancias reales del validador con datos de prueba

## Ejecución

```bash
dotnet test src/backend/AppBaseNetReact.Application.Tests
```
