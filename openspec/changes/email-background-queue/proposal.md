## Why

El plan original (sec. 5.1) especifica `"QueueEnabled": true // Uses Quartz.NET to queue emails`. El código ya incluye `EmailQueueService`, `EmailJob`, y `Quartz` como dependencia, pero nunca se ejecutan: los correos se envían sincrónicamente. Esto puede degradar la experiencia del usuario (el endpoint espera a que el SMTP responda) y aumentar la tasa de fallos si SMTP está temporalmente caído.

## What Changes

- `EmailBackgroundService` (implementando `BackgroundService`) que procese la cola de correos en segundo plano
- `EmailService` respete `EmailOptions.QueueEnabled` — si es true, encola en vez de enviar directo
- Limpieza de la dependencia Quartz si no se usa (o reemplazar con Quartz real)

## Capabilities

### New Capabilities
- `email-queue`: Sistema de cola de correos electrónicos con procesamiento background, reintentos configurables, y toggle via `QueueEnabled`

### Modified Capabilities
Ninguna

## Impact

- **Backend**: Nuevo `EmailBackgroundService` como `IHostedService`; modificación de `EmailService` para respetar `QueueEnabled`; registro en DI
- **Configuración**: `Email:QueueEnabled` ahora es funcional (antes era decorativo)
- **Tests**: Tests para background service, verificar que se encolen vs envíen directo según flag
