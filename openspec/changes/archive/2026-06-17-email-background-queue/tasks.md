## 1. Baseline — Regla de Oro

- [x] 1.1 Run `dotnet test AppBaseNetReact.slnx` — confirm 247/247 green
- [x] 1.2 Verify `EmailQueueService`, `EmailJob`, and `Quartz` NuGet reference exist but are never invoked

## 2. Backend — EmailBackgroundService

- [x] 2.1 Create `Infrastructure/Email/EmailBackgroundService.cs` — implement `BackgroundService`, consume `Channel<EmailMessage>` queue, process emails with retry
- [x] 2.2 Use `System.Threading.Channels.Channel<EmailMessage>` (unbounded) for the queue
- [x] 2.3 Implement retry logic: 3 attempts with exponential backoff (5s, 25s, 125s), discard + log on final failure
- [x] 2.4 Register as `IHostedService` in DI (`services.AddHostedService<EmailBackgroundService>()`)
- [x] 2.5 Run `dotnet build` — confirm 0 errors

## 3. Backend — Wire EmailService to queue

- [x] 3.1 Modify `EmailService.SendEmailAsync` to check `EmailOptions.QueueEnabled`:
  - If `true`: write to `Channel<EmailMessage>.Writer` and return immediately
  - If `false`: send synchronously (current behavior)
- [x] 3.2 Remove or keep Quartz dependency (decided in design — keep for now if referenced elsewhere, or remove if unused)
- [x] 3.3 Run `dotnet build` — confirm 0 errors

## 4. Tests — Email queue

- [x] 4.1 Create `Infrastructure.Tests/Email/EmailBackgroundServiceTests.cs`:
  - `QueueEnabled=true` → email is enqueued, service processes it
  - `QueueEnabled=false` → email is sent synchronously
  - Retry on SMTP failure, discard after max retries
  - Service recovers after transient failure
- [x] 4.2 Run `dotnet test` — confirm all tests pass

## 5. Final validation

- [x] 5.1 Run `dotnet build AppBaseNetReact.slnx` — 0 errors
- [x] 5.2 Run `dotnet test AppBaseNetReact.slnx` — all tests pass
