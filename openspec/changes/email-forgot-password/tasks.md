## 1. Backend: EmailOptions + Infrastructure Setup

- [x] 1.1 Create `EmailOptions` class in `Application/Common/Interfaces/` with SMTP settings, template config, retry policy, and queue flag — bind from `Email` section
- [x] 1.2 Create `EmailRenderer` service in `Infrastructure/Email/` that loads HTML templates as embedded resources and replaces `{{Variable}}` placeholders
- [x] 1.3 Create `EmailService` in `Infrastructure/Email/` implementing `IEmailService` with MailKit SMTP, Provider=None logger mode, retry logic
- [x] 1.4 Register `EmailService` + `EmailRenderer` + `EmailOptions` in `Infrastructure/DependencyInjection.cs`, conditional on Provider
- [x] 1.5 Create `EmailJob` + `EmailQueueService` in `Infrastructure/Email/` for queued background delivery

## 2. Backend: Email Templates

- [x] 2.1 Create `welcome.html` — responsive HTML with branding, login link, user name
- [x] 2.2 Create `email-confirmation.html` — confirmation link with token
- [x] 2.3 Create `password-reset.html` — reset link with token
- [x] 2.4 Create `password-changed.html` — notification with security contact info
- [x] 2.5 Create `temporary-password.html` — temp password + change instructions
- [x] 2.6 Create `account-locked.html` — lock notice + reset instructions

## 3. Backend: Auth Endpoints

- [x] 3.1 Fix `POST /api/auth/forgot-password` — remove temp password from response body, generate reset token, store hashed in User, send reset-link email
- [x] 3.2 Add `POST /api/auth/reset-password` — validate token + expiry, apply password policy, hash + store password, clear token, send changed notification
- [x] 3.3 Add `POST /api/auth/confirm-email` — validate confirmation token + expiry, confirm email, send welcome email
- [x] 3.4 Add email notification on `POST /api/auth/change-password` — send password-changed email
- [x] 3.5 Add email notification on account lock in `POST /api/auth/login` — send account-locked email when lockout triggers
- [x] 3.6 Add email notification on admin `PATCH /api/users/{id}/reset-password` — send temporary-password email

## 4. Frontend: Forgot/Reset Password Pages

- [x] 4.1 Create `ForgotPasswordPage` at `/forgot-password` with email input form and success/error states
- [x] 4.2 Create `ResetPasswordPage` at `/reset-password` with token from URL query param, new password form with validation
- [x] 4.3 Add routes for `/forgot-password` and `/reset-password` in the router (public, no auth required)
- [x] 4.4 Add "Forgot password?" link to LoginPage
- [x] 4.5 Install and configure sonner (toast library) for email-related notifications

## 5. Tests

- [x] 5.1 Unit tests for `EmailRenderer` — variable replacement, missing variable throws, template loading
- [x] 5.2 Unit tests for `EmailService` — SMTP send, retry logic, Provider=None mode
- [x] 5.3 Unit tests for `POST /api/auth/forgot-password` — email sent, no temp password in response, anti-enumeration
- [x] 5.4 Unit tests for `POST /api/auth/reset-password` — valid token, expired token, invalid token, password policy (via AuthController tests that include confirm-email patterns)
- [x] 5.5 Unit tests for `POST /api/auth/confirm-email` — valid token, expired token, invalid token
- [ ] 5.6 Integration test — forgot → reset flow end-to-end (requires PostgreSQL Testcontainer)
