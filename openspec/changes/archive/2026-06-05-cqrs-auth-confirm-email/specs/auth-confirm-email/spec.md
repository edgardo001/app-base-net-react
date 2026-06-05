## ADDED Requirements

### Requirement: Confirm Email With Valid Token
The system SHALL confirm a user's email address when a valid, unexpired confirmation token is presented, mark the user as `EmailConfirmed`, persist the change, and dispatch a welcome email.

#### Scenario: Valid token confirms the user
- **WHEN** a `POST /api/auth/confirm-email` request is made with a token that exists in the system and has not expired
- **THEN** the system MUST mark the user as `EmailConfirmed`, persist the change via `IUnitOfWork.SaveChangesAsync`, return HTTP 200 with an `ApiResponse<object>` containing message `"Email confirmed successfully"`, write an audit log entry with action `"EmailConfirmed"`, and send a welcome email using the `Welcome` template

#### Scenario: Audit log captures user id, IP, and user agent
- **WHEN** an email is successfully confirmed
- **THEN** the audit log entry MUST include `userId` (from the user entity), `ipAddress` from the HTTP request, and `userAgent` from the HTTP request

#### Scenario: Welcome email is dispatched via notification
- **WHEN** an email is successfully confirmed
- **THEN** the system MUST publish an `EmailConfirmedNotification` and the `EmailConfirmedEmailHandler` MUST invoke `IEmailService.SendWelcomeEmailAsync(userId, ct)` to send the welcome email asynchronously

### Requirement: Reject Invalid Confirmation Token
The system SHALL reject confirmation requests with malformed or unknown tokens by returning HTTP 400 with an anti-enumeration-safe error message and MUST NOT mutate user state or dispatch any side-effect notifications.

#### Scenario: Unknown token returns 400
- **WHEN** a `POST /api/auth/confirm-email` request is made with a token that does not exist in the system
- **THEN** the system MUST return HTTP 400 with an `ApiResponse<object>.Fail` containing message `"Invalid confirmation token"` and MUST NOT call `SaveChangesAsync`, write audit logs, or send emails

#### Scenario: Null user does not publish notifications
- **WHEN** the token lookup returns no user
- **THEN** the handler MUST NOT publish `EmailConfirmedNotification`

### Requirement: Reject Expired Confirmation Token
The system SHALL reject confirmation requests when the token exists but its `EmailConfirmationTokenExpires` is in the past, by returning HTTP 400 with message `"Confirmation token has expired"` and MUST NOT mutate user state or dispatch any side-effect notifications.

#### Scenario: Expired token returns 400
- **WHEN** a `POST /api/auth/confirm-email` request is made with a token that exists but whose `EmailConfirmationTokenExpires` is less than `IDateTimeProvider.UtcNow`
- **THEN** the system MUST return HTTP 400 with the expired message and MUST NOT call `SaveChangesAsync`, write audit logs, or send emails

#### Scenario: Expired token does not publish notifications
- **WHEN** the token expiry check fails
- **THEN** the handler MUST NOT publish `EmailConfirmedNotification`

### Requirement: Controller Maps Outcome To HTTP Response
The system SHALL map `EmailConfirmationResult` from the `ConfirmEmailCommand` to HTTP responses in the controller, without any business logic beyond outcome-to-HTTP translation.

#### Scenario: Success outcome returns 200
- **WHEN** `ConfirmEmailCommandHandler` returns `EmailConfirmationResult.Success()` (`EmailErrorCode.None`)
- **THEN** the controller MUST return HTTP 200 with `ApiResponse<object>.Ok(null, "Email confirmed successfully")`

#### Scenario: Failure outcome returns 400
- **WHEN** `ConfirmEmailCommandHandler` returns any `EmailConfirmationResult` with an error code other than `None`
- **THEN** the controller MUST return HTTP 400 with `ApiResponse<object>.Fail(outcome.Result.ErrorMessage!)`
