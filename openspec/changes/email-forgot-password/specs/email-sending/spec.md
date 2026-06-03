## ADDED Requirements

### Requirement: EmailService sends via SMTP using MailKit
The system SHALL implement `IEmailService` using MailKit's `SmtpClient` to deliver emails over SMTP with SSL/TLS.

#### Scenario: Successful email delivery
- **WHEN** `SendEmailAsync` is called with valid `to`, `subject`, and `htmlBody`
- **THEN** the email SHALL be delivered to the SMTP server and the method SHALL return without throwing

#### Scenario: SMTP connection failure
- **WHEN** the SMTP server is unreachable
- **THEN** the system SHALL retry up to `RetryCount` (default 3) times with `RetryDelaySeconds` (default 5) between attempts before throwing

#### Scenario: Invalid recipient email
- **WHEN** the recipient email address is malformed
- **THEN** the system SHALL throw `ArgumentException` with a descriptive message

### Requirement: Provider is None in development
The system SHALL support a `"Provider": "None"` mode that logs the email content via `ILogger` instead of sending.

#### Scenario: Development mode sending
- **WHEN** `Email:Provider` is `"None"`
- **THEN** `SendEmailAsync` SHALL log the recipient, subject, and body via Serilog and return successfully

#### Scenario: Development mode disabled on missing config
- **WHEN** `Email:Provider` is `"Smtp"` but SMTP host is empty
- **THEN** the system SHALL throw `InvalidOperationException` at first send attempt

### Requirement: EmailOptions configuration binding
The system SHALL bind `EmailOptions` from the `Email` section of `appsettings.json` with validation on startup.

#### Scenario: Valid configuration
- **WHEN** all required SMTP fields are populated
- **THEN** `EmailOptions` SHALL be validated and registered in DI

#### Scenario: Missing FromEmail
- **WHEN** `FromEmail` is empty or null
- **THEN** startup SHALL fail with a descriptive configuration error

### Requirement: Email sending via Quartz background job
The system SHALL support queuing emails for background delivery when `QueueEnabled` is true.

#### Scenario: Email queued for background delivery
- **WHEN** `QueueEnabled` is `true` and `SendEmailAsync` is called
- **THEN** the email SHALL be enqueued to a Quartz job and the method SHALL return immediately

#### Scenario: Queue disabled sends synchronously
- **WHEN** `QueueEnabled` is `false`
- **THEN** `SendEmailAsync` SHALL send the email synchronously (blocking call)
