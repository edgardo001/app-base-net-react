## 1. JWT Authentication

- [x] 1.1 Implement JwtService with GenerateAccessToken (HS512, 15 min, claims: sub, email, jti, firstName, lastName, permissions)
- [x] 1.2 Implement JwtSettings with configuration binding (SecretKey, Issuer, Audience, AccessTokenExpirationMinutes, RefreshTokenExpirationDays, ClockSkewSeconds)
- [x] 1.3 Configure JWT authentication in DI: AddAuthentication + AddJwtBearer with validation parameters
- [x] 1.4 Clear default inbound claim type map to preserve clean JWT claims
- [x] 1.5 Set MapInboundClaims = false

## 2. Password Hashing

- [x] 2.1 Implement PasswordHasherService with PBKDF2 (SHA-256, 128-bit salt, 256-bit key, 100k iterations)
- [x] 2.2 Implement HashPassword with salt+hash storage format
- [x] 2.3 Implement VerifyPassword with constant-time comparison (CryptographicOperations.FixedTimeEquals)

## 3. Password Policy Service

- [x] 3.1 Implement PasswordPolicyService with configurable rules (length, uppercase, lowercase, digit, special)
- [x] 3.2 Implement PasswordPolicySettings with all configurable parameters
- [x] 3.3 Implement Validate method returning (Valid, Error) tuple

## 4. Auth Controller

- [x] 4.1 Implement POST /api/auth/login with credential validation, lockout check, token generation, audit logging
- [x] 4.2 Implement POST /api/auth/refresh with token rotation and reuse detection
- [x] 4.3 Implement POST /api/auth/logout with token revocation
- [x] 4.4 Implement POST /api/auth/change-password with current password verification and session revocation
- [x] 4.5 Implement POST /api/auth/forgot-password with temporary password generation
- [x] 4.6 Apply EnableRateLimiting("Login") and EnableRateLimiting("ForgotPassword") attributes

## 5. Rate Limiting

- [x] 5.1 Configure AddRateLimiter with Login policy (10/min, fixed window)
- [x] 5.2 Configure ForgotPassword policy (3/hr, fixed window)
- [x] 5.3 Configure Global policy (100/min, fixed window)
- [x] 5.4 Set RejectionStatusCode = 429
- [x] 5.5 Place UseRateLimiter between CORS and Authentication in pipeline

## 6. Security Headers Middleware

- [x] 6.1 Implement SecurityHeadersMiddleware using context.Response.OnStarting()
- [x] 6.2 Add X-Frame-Options: DENY
- [x] 6.3 Add X-Content-Type-Options: nosniff
- [x] 6.4 Add X-XSS-Protection: 1; mode=block
- [x] 6.5 Add Referrer-Policy: strict-origin-when-cross-origin
- [x] 6.6 Add Permissions-Policy: camera=(self), microphone=()
- [x] 6.7 Add Content-Security-Policy with Cloudflare Turnstile allowlist

## 7. Exception Handling Middleware

- [x] 7.1 Implement ExceptionHandlingMiddleware catching all exceptions
- [x] 7.2 Map ValidationException → 400 with error details
- [x] 7.3 Map UnauthorizedAccessException → 403
- [x] 7.4 Map KeyNotFoundException → 404
- [x] 7.5 Map all other exceptions → 500 with sanitized message (no stack trace)
- [x] 7.6 Register as first middleware in pipeline

## 8. Audit Service

- [x] 8.1 Implement AuditService.LogAsync creating AuditLog entities
- [x] 8.2 Register audit events for login success/failure, logout, password change, token revocation
- [x] 8.3 Implement LoginAttempt recording for all login attempts

## 9. Development Configuration

- [x] 9.1 Create appsettings.Development.json with relaxed password policy for development
- [x] 9.2 Set higher rate limits in development (Login 100/min, Global 1000/min)
- [x] 9.3 Configure development JWT secret key (64+ chars)
