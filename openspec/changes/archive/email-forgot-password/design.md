## Context

The project has MailKit (4.17.0) and Quartz (3.18.1) NuGet packages already installed in the Infrastructure layer. The `IEmailService` interface exists with a single `SendEmailAsync(string to, string subject, string htmlBody)` method. The `appsettings.json` has a full email configuration section with SMTP settings, 6 template definitions, retry policy, and queue toggle. The development profile sets `"Provider": "None"` to skip email in local dev.

The `ForgotPassword` endpoint (`POST /api/auth/forgot-password`) is partially implemented: it generates a temp password, hashes it into the user record, and returns it **in the response body** (`new { TemporaryPassword = tempPassword }`) with a TODO comment: `// Send email with temp password when EmailService is configured`. This is a security concern.

No email sending infrastructure exists beyond the interface. No template files, no background job, no EmailOptions configuration class.

## Goals / Non-Goals

**Goals:**
- Implement `IEmailService` using MailKit SMTP with configurable provider
- Create an `EmailOptions` configuration class bound to `appsettings.json:Email`
- Create 6 responsive HTML email templates with variable interpolation (`{{UserName}}`, `{{TempPassword}}`, etc.)
- Fix `ForgotPassword` endpoint: remove temp password from response, send via email
- Add `POST /api/auth/confirm-email` endpoint for token-based email verification
- Add `POST /api/auth/reset-password` endpoint for token-based password reset
- Add email notification on password change and account lock
- Register email service and background job in dependency injection
- Create Forgot Password page in frontend
- Add toast/sonner notifications for email-related actions (confirmation sent, password reset, etc.)

**Non-Goals:**
- Avatar upload/storage (separate change)
- Captcha/Turnstile implementation (separate change)
- CQRS migration (separate change)
- 2FA / MFA
- Email queue persistence (in-memory queue via Quartz is sufficient for MVP)

## Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **Template engine** | Simple string replace (`{{Variable}}`) | No external dependency needed for MVP. 6 templates with ~5 variables each. Can migrate to Razor/Rainbow later. |
| **Email provider abstraction** | `IEmailService` with conditional registration | Dev uses `"Provider": "None"` (no-op logger). Production uses SMTP. No need for complex factory pattern. |
| **Background queue** | Quartz.NET job with in-memory store | Package already installed. `QueueEnabled` flag controls whether to send sync or via queue. For MVP, synchronous sending with retry is sufficient; queue is future-ready. |
| **Template location** | `Infrastructure/Email/Templates/*.html` | Embedded resources loaded at startup. No file system dependency in containers. |
| **Confirm-email token** | Short-lived JWT-like random token (32 chars, 24h expiry) | Stored in User entity (`EmailConfirmationToken` + `EmailConfirmationTokenExpires`), same pattern as existing temp password. |
| **Reset-password flow** | Token-based (not temp-password) | User requests reset → receives email with reset token → `POST /reset-password` with token + new password. More secure than emailing a password. |

```
┌──────────────────────────────────────────────────────────────────┐
│                   EMAIL FLOW DIAGRAM                             │
└──────────────────────────────────────────────────────────────────┘

Forgot Password:
  POST /api/auth/forgot-password (email)
    → Generate reset token (32 chars), store in User
    → EmailService.SendEmailAsync(to, subject, template)
      → EmailRenderer.Render("password-reset.html", { UserName, ResetLink })
      → SmtpClient.SendAsync()
    → Return 200 (always, anti-enumeration)

Reset Password:
  POST /api/auth/reset-password (token, newPassword)
    → Find user by reset token + validate expiry
    → Hash + store new password
    → Clear token, force password change flag
    → Send "password-changed" notification email
    → Return 200

Confirm Email:
  POST /api/auth/confirm-email (token)
    → Find user by confirmation token + validate expiry
    → user.ConfirmEmail() (clears token, sets confirmed)
    → Send welcome email
    → Return 200

Email Queue (Quartz):
  ┌──────────┐    ┌──────────────┐    ┌──────────────┐
  │ SendAsync │───▶│ Quartz Job   │───▶│ SmtpClient   │
  │ (writes   │    │ (retry up to │    │ SendAsync    │
  │  to queue)│    │  3x, 5s gap)│    │              │
  └──────────┘    └──────────────┘    └──────────────┘
```

## Risks / Trade-offs

- **[Security] Reset token in URL** → Tokens are single-use, short-lived (24h), stored hashed. Only sent via email (not in API response).
- **[Deliverability] SMTP without dedicated service** → Gmail SMTP has daily limits (~500). For production, use SendGrid/Mailgun/SES instead. The `IEmailService` interface makes this swappable.
- **[Template maintenance] String replacement is fragile** → If a template references `{{MissingVar}}`, it renders literally. Solution: validate all variables exist during render and throw if missing.
- **[Rate limiting] ForgotPassword already rate limited** → 3 requests/hour per IP. This prevents email bombing. Already configured.
