## Why

The forgot-password flow currently generates a temporary password and returns it directly in the HTTP response body — a security anti-pattern that exposes credentials in transit logs and breaks the intended user experience. Email sending (welcome, confirmation, password reset, alerts) is completely unimplemented despite the infrastructure being provisioned (MailKit + Quartz packages installed, appsettings configured with SMTP and 6 email templates). Users cannot confirm their email, reset passwords via email, or receive any system notifications.

## What Changes

- **Fix ForgotPassword security**: Remove temp password from response body, send it via email instead
- **Implement EmailService**: MailKit SMTP implementation with template rendering, retry logic, and HTML template engine
- **Create email templates**: 6 HTML templates (welcome, email-confirmation, password-reset, password-changed, temporary-password, account-locked)
- **Add confirm-email endpoint**: `POST /api/auth/confirm-email` with token validation
- **Add reset-password endpoint**: `POST /api/auth/reset-password` with token validation
- **Add change-password email notification**: Send confirmation when password changes
- **Add login email notification**: Send alert when account is locked
- **Email queue via Quartz.NET**: Background job for reliable email delivery with retries
- **Frontend**: Forgot Password page, email confirmation UI feedback

## Capabilities

### New Capabilities
- `email-sending`: SMTP email delivery via MailKit with HTML templates, retry logic, and queue via Quartz.NET
- `forgot-password-flow`: Complete forgot/reset password flow with email delivery of temporary password
- `email-confirmation`: Email confirmation on registration with token-based verification endpoint
- `email-templates`: 6 responsive HTML email templates with branding variables

### Modified Capabilities
(none — first set of capabilities for this project)

## Impact

- **Backend**: New `Infrastructure/Email/` folder with EmailService, template renderer, template files. New `AuthController` endpoints (`confirm-email`, `reset-password`). ForgotPassword endpoint modified to remove temp password from response.
- **Frontend**: New `ForgotPasswordPage` at `/forgot-password`. Toast notifications for email actions.
- **Config**: Email settings already defined in appsettings.json — no config changes needed.
- **DI**: Register `IEmailService` → `EmailService` in Infrastructure/DependencyInjection.
- **Security**: Temp password no longer leaks in API responses. Rate limiting already applied to forgot-password endpoint.
