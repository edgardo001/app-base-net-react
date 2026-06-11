## Context

El código ya incluye `EmailQueueService` (cola en memoria), `EmailJob` (procesador de la cola), y `Quartz` como dependencia NuGet. Sin embargo, ningún `BackgroundService` o `IHostedService` invoca `ProcessQueueAsync()`. Los correos se envían sincrónicamente a través de `EmailService.SendEmailAsync`. La configuración `Email:QueueEnabled = true` es decorativa.

## Goals / Non-Goals

**Goals:**
- Crear `EmailBackgroundService` (implementando `BackgroundService`) que procese la cola de correos en loop cada N segundos
- Modificar `EmailService` para que cuando `QueueEnabled = true` encole en vez de enviar directo
- Cuando `QueueEnabled = false`, mantener el comportamiento actual (envío sincrónico)
- Agregar reintentos configurables con backoff exponencial
- Tests unitarios que verifiquen ambas rutas (síncrona vs cola)

**Non-Goals:**
- No se implementa persistencia de la cola en BD (se mantiene en memoria)
- No se implementa Quartz.NET scheduling (se reemplaza por BackgroundService simple)
- No se implementan dead-letter queues ni notificaciones de fallo permanente

## Decisions

### BackgroundService vs Quartz.NET
- **Decisión**: Usar `BackgroundService` + `Channel<EmailMessage>` en lugar de Quartz
- **Rationale**: Quartz está referenciado pero nunca usado. Un `BackgroundService` con `Channel<T>` es más simple, no requiere BD, y cubre el caso de uso (cola en memoria). Si se necesita persistencia en el futuro, se migra a Quartz con BD
- **Alternativa A**: Quartz real con store en BD — sobreingeniería para el MVP
- **Alternativa B**: Queue sincrónico sin background — no resuelve el problema de degradación de UX

### Channel<T> vs BlockingCollection vs ConcurrentQueue
- **Decisión**: `System.Threading.Channels.Channel<EmailMessage>` unbounded
- **Rationale**: API moderna async-first, soporta producción/consumo con backpressure, no requiere locks manuales
- **Alternativa**: `ConcurrentQueue<EmailMessage>` + `ManualResetEvent` — más verboso y propenso a errores

## Risks / Trade-offs

- [Risk] Cola en memoria se pierde si el proceso se reinicia → Mitigación: aceptado para MVP. Se puede migrar a cola persistente (BD / RabbitMQ) en el futuro
- [Risk] Un email que falla reintenta infinitamente → Mitigación: max 3 reintentos con backoff exponencial (5s, 25s, 125s); después se loggea y descarta
- [Trade-off] Encolar vs enviar directo: encolar mejora UX pero retrasa la entrega. Para el MVP, se deja `QueueEnabled: false` por defecto, true en producción
