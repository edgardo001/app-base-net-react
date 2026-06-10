# Behaviors — Pipeline Behaviors de MediatR

| Archivo | Propósito |
|---------|-----------|
| `ValidationBehavior.cs` | Pipeline behavior que ejecuta `FluentValidation` validators automáticamente antes de cada Command/Query de MediatR |

> ✅ Activo para todos los Commands/Queries que tengan un validator FluentValidation registrado. Se ejecuta automáticamente gracias al pipeline de MediatR.
