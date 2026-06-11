## ADDED Requirements

### Requirement: Email background processing
The system SHALL process email sending in the background when `Email:QueueEnabled` is `true`, using a `BackgroundService` that drains a `Channel<EmailMessage>` queue.

#### Scenario: QueueEnabled=true enqueues instead of sending
- **WHEN** `Email:QueueEnabled` is `true` and a notification handler calls `SendEmailAsync`
- **THEN** the email is enqueued in the `Channel<EmailMessage>` queue
- **AND** the method returns immediately without waiting for SMTP

#### Scenario: Background service processes queue
- **WHEN** the `EmailBackgroundService` is running and there are emails in the queue
- **THEN** it processes them one by one, calling `IEmailSender.SendAsync` for each
- **AND** it polls the queue every 2 seconds when empty

#### Scenario: QueueEnabled=false sends synchronously
- **WHEN** `Email:QueueEnabled` is `false` and a notification handler calls `SendEmailAsync`
- **THEN** the email is sent synchronously via SMTP (current behavior)

### Requirement: Retry with backoff on failure
The system SHALL retry failed email sends up to 3 times with exponential backoff.

#### Scenario: Failed send is retried
- **WHEN** SMTP returns an error on the first attempt
- **THEN** the system retries after 5 seconds
- **AND** if it fails again, retries after 25 seconds
- **AND** if it fails a third time, retries after 125 seconds
- **AND** if all 3 retries fail, the email is discarded and an error is logged

#### Scenario: Successful send stops retries
- **WHEN** a retry attempt succeeds
- **THEN** the email is marked as sent and no further retries occur
