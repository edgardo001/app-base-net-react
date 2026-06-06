## Context

The platform has a complete email infrastructure: `IEmailService` (MailKit SMTP), `EmailRenderer` (embedded HTML templates with `{{Variable}}` placeholders), and 6 existing templates. The AdminController (restricted to SuperAdmin) currently provides dashboard stats, audit log, and global token revocation. There is no way for admins to test email delivery without triggering a real event.

## Goals / Non-Goals

**Goals:**
- SuperAdmin can send a test email to any address from the `/admin` page
- A new email template `test-email.html` confirms the sender, timestamp, and SMTP config
- Success/failure feedback via toast on the frontend
- Audit logging of the test email action

**Non-Goals:**
- No rate limiting on this endpoint (admin-only, low volume)
- No template selection in the UI (just a simple test)
- No email queue bypass (uses the standard `IEmailService` with its retry logic)

## Decisions

1. **New endpoint in AdminController vs separate controller** → AdminController. It's already SuperAdmin-restricted and the logical home.
2. **New template `test-email.html` vs inline body** → New template. Follows the existing pattern, consistent with the renderer infrastructure.
3. **Frontend: inline form vs modal** → Inline form section below the audit log card. Simpler UX, consistent with the existing single-page layout. A Card with an input and button is cleaner than a modal for a single field.
4. **Toast library** → Uses the existing `sonner` pattern (toaster component already present in the app shell). `toast.success()` / `toast.error()`.
5. **DTO for request** → Simple `{ to: string }` body, validated with FluentValidation inline or `[Required]` + `[EmailAddress]` data annotation.

## Risks / Trade-offs

- **SMTP misconfiguration** → The email service already handles this with retry logic and throws on `Host == ""`. The error propagates to the API response and is shown in the toast.
- **Dev mode ("None" provider)** → The service logs to console. The response will indicate success even though no real email was sent. Acceptable: the admin can see the log confirms the flow works.
- **No test email template exists** → Need to add it as an embedded resource. Follows existing pattern in `EmailRenderer`.
