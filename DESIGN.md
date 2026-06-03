# Architecture Decision Records (ADR) — User Management Platform

> Every significant architectural decision documented with context, options, decision, rationale, and trade-offs.

---

## 1. Hexagonal Architecture (Ports & Adapters)

**Context:** Need a maintainable, testable architecture that separates business logic from infrastructure concerns.

**Options:**
1. Pure hexagonal (Composition Root project, strict dependency rules)
2. Pragmatic hexagonal (WebApi references Infrastructure directly)
3. Traditional n-tier (Controller → Service → Repository)
4. Vertical Slices with MediatR

**Decision:** Pragmatic hexagonal — 4 projects (Domain, Application, Infrastructure, WebApi) with WebApi referencing Infrastructure for startup bootstrapping.

**Rationale:**
- Domain has zero NuGet dependencies — pure business logic
- Application depends only on Domain + MediatR/FluentValidation
- Infrastructure implements Application interfaces (ports)
- WebApi references Infrastructure because ASP.NET requires `AddDbContext`, JWT auth, and service registration at startup
- Full hexagonal would require a separate Composition Root project — added complexity without proportional benefit for this scale

**Trade-offs:**
- Pure hexagonal would isolate WebApi from Infrastructure completely, preventing accidental coupling in controllers
- The pragmatic approach accepts that controllers CAN reference infrastructure types (but convention discourages it)
- If the project grows significantly, extracting a Composition Root is a straightforward refactor

**Date:** 2026-06-02

---

## 2. BaseEntity with Soft Delete and Concurrency

**Context:** Every entity needs audit fields (creation, modification, deletion) and optimistic concurrency control.

**Options:**
1. BaseEntity abstract class with all common fields
2. IEntity interface with manual implementation per entity
3. No base — each entity defines its own fields

**Decision:** Abstract `BaseEntity` with `Guid Id`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, `DeletedAt`, `ConcurrencyToken`.

**Rationale:**
- GUID primary keys enable distributed ID generation without DB roundtrip and prevent sequential ID enumeration
- `DateTime.UtcNow` avoids timezone ambiguity
- Soft delete (`DeletedAt`) enables data recovery and audit trail; global query filters automatically exclude deleted rows
- `ConcurrencyToken` (byte[], `IsConcurrencyToken()`) prevents lost updates in concurrent scenarios
- `protected set` on all properties enforces domain encapsulation — state changes only through behavioral methods
- Abstract class forces consistency — no entity can "forget" to add audit fields

**Trade-offs:**
- Every entity carries ~56 bytes of audit overhead per row
- Non-BaseEntity types (e.g., value objects) cannot use GenericRepository
- EF Core needs `Property().CurrentValue` to set `UpdatedAt` due to protected setter

**Date:** 2026-06-02

---

## 3. GenericRepository + UnitOfWork

**Context:** Need consistent data access with cross-cutting concerns (soft delete, pagination) and testability.

**Options:**
1. GenericRepository with BaseEntity constraint + IUnitOfWork
2. Direct DbSet access from controllers
3. DbContext directly injected
4. Repository per entity without generic base

**Decision:** `GenericRepository<T> where T : BaseEntity` with `IUnitOfWork` aggregating all repositories.

**Rationale:**
- `DeleteAsync()` consistently calls `SoftDelete()` — cannot be forgotten by any caller
- `GetPagedAsync()` provides reusable pagination with dynamic sorting — reduces boilerplate across 6+ entities
- `IUnitOfWork` aggregates all repositories behind a single injectable interface — avoids constructor explosion (controllers would need 5-6 repository parameters)
- Lazy repository instantiation — only created when first accessed
- Testable: `Mock<IUserRepository>` replaces the database in unit tests

**Trade-offs:**
- Generic Repository is sometimes considered an anti-pattern with EF Core (DbContext already implements Unit of Work + Repository)
- The abstraction layer adds indirection for simple CRUD operations
- If query complexity grows significantly, CQRS with separate read models would be more performant

**Date:** 2026-06-02

---

## 4. JWT Authentication with Refresh Token Rotation

**Context:** Need stateless authentication with secure session management and compromise detection.

**Options:**
1. JWT access + refresh token (current)
2. Session cookies (stateful)
3. OAuth2 / OpenID Connect
4. API Keys

**Decision:** HS512 JWT with 15-min access token + 7-day refresh token, rotation on each use, SHA-256 hashed storage.

**Rationale:**
- Short-lived access tokens (15 min) limit exposure if stolen
- Refresh token rotation: each refresh issues a new refresh token and revokes the old one
- If a revoked token is reused → all user sessions are revoked (compromise detection)
- Refresh tokens are SHA-256 hashed before storage — never stored raw
- `CryptographicOperations.FixedTimeEquals` prevents timing attacks during comparison
- JWT `jti` (token ID) enables individual token tracking and revocation

**Trade-offs:**
- Stateful refresh token storage requires DB lookup on every refresh
- No sliding sessions — user must explicitly refresh every 15 minutes
- HS512 requires minimum 64-byte secret key; RS256 would enable key rotation without redeployment

**Date:** 2026-06-02

---

## 5. FluentValidation (without Auto-Validation Middleware)

**Context:** Need declarative, composable validation for all API inputs.

**Options:**
1. FluentValidation with `AddFluentValidationAutoValidation()` (chosen initially, then removed)
2. FluentValidation with MediatR pipeline behavior
3. Data annotations on DTOs
4. Manual validation in controllers

**Decision:** FluentValidation validators registered in DI, usable via MediatR `ValidationBehavior` or explicit controller calls. Auto-validation middleware removed due to version conflict.

**Rationale:**
- FluentValidation provides complex rules (conditional, cross-property) that data annotations cannot express
- Validators are co-located with DTOs in `Application.Common.Validators` namespace — single source of truth
- MediatR pipeline behavior runs validators automatically before any handler
- `[ApiController]` + implicit model binding still provides basic required-field validation
- `FluentValidation.AspNetCore` v11 was incompatible with FluentValidation v12 — removed to fix `TypeLoadException`
- Validators remain registered in DI (`AddValidatorsFromAssemblyContaining<>`) for future auto-validation re-implementation

**Trade-offs:**
- Without auto-validation middleware, validators must be triggered explicitly or via MediatR pipeline
- Model binding errors use ASP.NET's default error format, not FluentValidation's structured errors
- A custom `ValidationFilter` or MediatR behavior is needed for seamless integration

**Date:** 2026-06-02

---

## 6. Explicit AuditService vs Automatic Interception

**Context:** Need detailed audit logging for all critical operations with semantic context.

**Options:**
1. Explicit `IAuditService.LogAsync()` calls from controllers
2. EF Core `SaveChangesInterceptor` — auto-log all changes
3. Middleware — log request/response pairs
4. Database triggers

**Decision:** Explicit audit calls from controllers with old/new value serialization.

**Rationale:**
- Controllers know what semantic action occurred (e.g., "RolePermissionsUpdated" vs "RoleCreated") — middleware and interceptors only see raw data changes
- Controllers can capture old values BEFORE mutation and new values AFTER — automatic interception would need complex diffing
- Controllers have access to HTTP context (IP, UserAgent) for rich audit metadata
- Opt-in, not automatic: not every DB change needs an audit log
- Audit logs include both identity (`UserId`) and request context (`IpAddress`, `UserAgent`)

**Trade-offs:**
- Developers must remember to call `_audit.LogAsync()` — mitigated by code review patterns
- ~10 lines of audit boilerplate per mutating action — acceptable for clarity
- No automatic coverage — an action without an audit call will not be logged

**Date:** 2026-06-02

---

## 7. Rate Limiting with Named Policies

**Context:** Need to prevent brute force attacks and API abuse.

**Options:**
1. ASP.NET Core built-in rate limiting (chosen)
2. Custom middleware
3. Reverse proxy rate limiting (Traefik/Nginx)
4. Database-level rate limiting

**Decision:** Three named fixed-window policies configured in `appsettings.json` and applied via `[EnableRateLimiting]` attributes.

**Rationale:**
- Built-in ASP.NET Core rate limiting is zero-dependency and integrates with the middleware pipeline
- Named policies enable different limits per endpoint:
  - **Login:** 10 requests/minute — aggressive brute force prevention
  - **ForgotPassword:** 3 requests/hour — prevent email flooding
  - **Global:** 100 requests/minute with queue of 2 — baseline API protection
- Partitioned by IP (`RemoteIpAddress`) — fair per-client limiting
- Configuration-driven — limits can be changed without code changes

**Trade-offs:**
- In-memory rate limiting resets on server restart — not suitable for multi-instance deployments
- No distributed rate limiting (would need Redis or similar)
- Fixed-window has "burst at window edge" problem (sliding window would be smoother)

**Date:** 2026-06-02

---

## 8. Security Headers via Custom Middleware

**Context:** Need defense-in-depth security headers on all responses.

**Options:**
1. Custom `SecurityHeadersMiddleware` (chosen)
2. web.config — irrelevant for Kestrel
3. NWebsec package — additional dependency
4. Hosting platform level (Traefik/Nginx)

**Decision:** Custom middleware attached to `Response.OnStarting()` callback.

**Rationale:**
- Kestrel-native — no dependency on IIS or hosting platform
- `Response.OnStarting()` ensures headers are set even if response is modified downstream
- CSP includes Cloudflare Turnstile domains (`https://challenges.cloudflare.com`) for captcha support
- All headers applied before any response body is written
- Easy to conditionally adjust headers per environment (dev vs prod CSP)

**Trade-offs:**
- Adds ~1ms per request for header computation
- Custom middleware must be maintained; NWebsec would handle edge cases (e.g., framing for PDF viewers)

**Date:** 2026-06-02

---

## 9. Zustand for State Management (Frontend)

**Context:** Need simple, performant global state for authentication without excessive boilerplate.

**Options:**
1. Zustand (chosen)
2. Redux Toolkit — boilerplate-heavy for a single domain
3. React Context — re-renders all consumers on any change
4. Jotai — simpler but less mature ecosystem

**Decision:** Single Zustand store (`useAuthStore`) for authentication state.

**Rationale:**
- Zero providers — no `<Provider>` wrapping needed at app root
- Selector-based subscriptions — components only re-render when their specific slice changes (`useAuthStore((s) => s.user)`)
- ~60 lines for the entire store (login, logout, checkAuth, user, permissions)
- Built-in async action support — no middleware like Redux Thunk
- The app only has ONE domain of global state (auth) — Zustand is proportional to the problem

**Trade-offs:**
- Single store file becomes a god object if the app grows
- No built-in devtools (can be added with middleware)
- No built-in normalization or caching — server data is managed per-page with `useState`

**Date:** 2026-06-02

---

## 10. Axios with Request/Response Interceptors

**Context:** Need automatic auth header injection and transparent token refresh.

**Options:**
1. Axios with interceptors (chosen)
2. Fetch API wrapper — less ergonomic for interceptors
3. React Query — heavier, not needed for simple requests

**Decision:** Pre-configured Axios instance with request interceptor (Bearer token) + response interceptor (401 handling with refresh queue).

**Rationale:**
- Request interceptor injects `Authorization: Bearer <token>` from localStorage — single source of truth
- Response interceptor detects 401, queues concurrent failed requests (only one refresh call for N simultaneous 401s), replays with new token
- Refresh failure → clear storage → redirect to login
- No framework coupling — Axios is independent of React/Zustand

**Trade-offs:**
- Direct localStorage access — not SSR-compatible
- Two separate refresh implementations (interceptor + session-warning component)
- Refresh endpoint URL hardcoded in interceptor

**Date:** 2026-06-02

---

## 11. Tailwind v4 + shadcn/ui v4

**Context:** Need a modern, customizable UI framework with good DX and small bundle size.

**Options:**
1. Tailwind v4 + shadcn/ui v4 (chosen)
2. MUI — heavy (~100KB gzipped), hard to override styles
3. Ant Design — heavy, Chinese-market UX patterns
4. Chakra UI — equivalent DX but more runtime cost

**Decision:** Tailwind CSS v4 (CSS-first config, no `tailwind.config.js`) + shadcn/ui v4 (Base UI primitives, copy-paste component model).

**Rationale:**
- Tailwind v4: `@import "tailwindcss"` — zero config, `@theme inline { }` for custom tokens, OKLCH color space
- shadcn/ui v4: components are local source files (not npm packages) — fully customizable without fighting upstream
- Base UI primitives (by Radix team): better accessibility with `data-*` attributes, smaller bundles via tree-shaking
- OKLCH colors: perceptually uniform — better dark mode transitions than HSL/RGB
- CVA (`class-variance-authority`) for variant-based component styling

**Trade-offs:**
- No `tailwind-merge` — conflicting Tailwind classes not automatically deduplicated
- Copy-paste upgrade model — updating shadcn requires re-running CLI
- No premium themes — every color token must be hand-tuned

**Date:** 2026-06-02

---

## 12. Test Infrastructure (xUnit + Moq + FluentAssertions)

**Context:** Need a robust, expressive testing stack for unit and integration tests.

**Options:**
1. xUnit + Moq + FluentAssertions (chosen)
2. NUnit — legacy attribute patterns, slower modern .NET adoption
3. MSTest — less extensible, weaker assertion library integration

**Decision:** xUnit 2.9.3 (constructor-based setup, parallel execution) + Moq 4.20.72 (fluent `Setup().Returns()`, `Verify()`) + FluentAssertions 8.10.0 (expressive chainable assertions).

**Rationale:**
- xUnit: constructor-based setup (no `[SetUp]`/`[TearDown]` attributes) — cleaner OOP alignment
- xUnit: built-in parallel test execution by default
- Moq: >60% .NET market share — largest knowledge base
- FluentAssertions: `result.Should().Be(expected)` reads naturally compared to `Assert.Equal(expected, result)`
- FluentAssertions: descriptive failure messages dramatically speed debugging
- Test project naming mirrors source: `tests/AppBaseNetReact.{Layer}.Tests/`

**Trade-offs:**
- Moq 4.x is the legacy pipeline; 5.x (still preview) uses .NET Castle.Core differently
- FluentAssertions 8.x dropped support for some legacy assertion methods

**Date:** 2026-06-02

---

## 13. Docker Multi-Stage Builds with Alpine

**Context:** Need minimal, secure container images for production deployment.

**Options:**
1. Alpine-based multi-stage (chosen)
2. Ubuntu-based — 3x larger images
3. Distroless — harder to debug

**Decision:** Alpine for both SDK (build) and ASP.NET (runtime) stages. Frontend: Node Alpine → nginx Alpine.

**Rationale:**
- SDK → Runtime: ~1.7GB → ~200MB (8.5x reduction)
- Alpine has smaller attack surface (musl libc, minimal packages)
- NuGet layer caching: `.csproj` files copied and restored first — layer cache only busted when dependencies change
- Frontend: 25MB final image (nginx + static HTML/JS/CSS)
- `npm ci` instead of `npm install` — deterministic, fails if lockfile out of sync

**Trade-offs:**
- Alpine uses musl instead of glibc — rare compatibility issues with some .NET native dependencies
- Debugging in Alpine requires apk-installing debugging tools

**Date:** 2026-06-02

---

## 14. Traefik as Reverse Proxy

**Context:** Need automatic TLS, Docker-native routing, and minimal config.

**Options:**
1. Traefik (chosen)
2. nginx-proxy + certbot companion — two containers, more complex
3. Caddy — less mature Docker integration
4. Manual certbot + nginx — operational overhead

**Decision:** Traefik v3.3 with Docker provider, automatic Let's Encrypt via TLS-ALPN-01 challenge.

**Rationale:**
- Docker-native: reads container labels for routing — no manual config files
- Built-in ACME: automatic certificate issuance and renewal — no certbot cron jobs
- Label-based: `traefik.http.routers.backend.rule=Host(...)` co-located with service definition
- `exposedbydefault=false` — security: containers must explicitly opt in

**Trade-offs:**
- Traefik's configuration model (labels + dynamic config) has a learning curve
- For simple deployments, nginx-proxy is more widely understood

**Date:** 2026-06-02

---

## 15. Nginx for SPA Serving

**Context:** Need production-grade static file serving with SPA fallback routing.

**Options:**
1. nginx:alpine (chosen)
2. Node Express — unnecessary runtime dependency
3. Apache — heavier, more complex config

**Decision:** nginx:alpine with SPA fallback (`try_files $uri $uri/ /index.html`) and aggressive asset caching.

**Rationale:**
- `try_files $uri $uri/ /index.html` — React Router handles client-side routing; refreshing `/users` returns SPA instead of 404
- `expires 1y; Cache-Control: public, immutable` — Vite generates hashed filenames (`main.a1b2c3.js`), so assets never need revalidation
- Gzip for text-based MIME types only (not already-compressed images)
- No TLS in nginx — Traefik handles that upstream

**Trade-offs:**
- No brotli compression (nginx:alpine doesn't include brotli module by default)
- No HTTP/2 push — modern browsers don't need it

**Date:** 2026-06-02

---

## 16. Middleware Pipeline Order

**Context:** The order of middleware in `Program.cs` affects security, performance, and correctness.

**Decision:**
```
1. ExceptionHandlingMiddleware    — wraps everything below
2. SecurityHeadersMiddleware      — set headers on ALL responses
3. CORS                          — before auth (preflight doesn't need auth)
4. RateLimiter                   — before auth (reject abusers before auth work)
5. Authentication                — establish identity
6. Authorization                 — enforce policies
7. Controllers                   — route to handlers
```

**Rationale:**
- Exception handling first: catches errors from ALL downstream middleware, not just controllers
- Security headers before CORS: headers set even on rejected/preflight requests
- Rate limiting before auth: reject excessive requests without spending auth validation resources
- Auth before controllers: identity established before business logic runs

**Trade-offs:**
- Exception middleware cannot catch exceptions from middleware registered before it
- Static files (if ever added) should go before security headers for performance

**Date:** 2026-06-02

---

## 17. Frontend Proxy vs Backend CORS

**Context:** During development, frontend (port 5173) and backend (port 5011) run on different origins.

**Options:**
1. Vite dev server proxy (chosen)
2. Backend CORS middleware with `AllowCredentials()`
3. Both (current: proxy only)

**Decision:** Vite proxy forwards `/api/*` and `/scalar/*` to `http://localhost:5011` with `changeOrigin: true`.

**Rationale:**
- Makes requests same-origin from browser's perspective — no CORS headers needed in development
- Single port for developers — access `http://localhost:5173` for everything
- Production uses Traefik/nginx for the same proxy behavior
- Backend CORS config exists but is not strictly required during development

**Trade-offs:**
- Backend CORS config still needed for production if frontend and backend are on different domains
- Proxy is Vite-specific — switching dev servers would require reconfiguration

**Date:** 2026-06-02

---

## 18. Password Policy as Separate Service

**Context:** Password validation rules must be configurable, testable, and reusable across endpoints.

**Options:**
1. `IPasswordPolicyService` with `IOptions<PasswordPolicySettings>` (chosen)
2. Inline validation in controllers — not reusable, harder to test
3. Validation in User entity — violates domain zero-dependency rule

**Decision:** Dedicated `PasswordPolicyService` implementing `IPasswordPolicyService`, injected into controllers. Policy parameters from `appsettings.json`.

**Rationale:**
- Configuration-driven: all rules (length, complexity, expiration, lockout) are configurable without code changes
- Isolated: unit-testable independently of controllers and entities
- Reusable: called from `AuthController.ChangePassword()`, `AuthController.ForgotPassword()`, future registration endpoint
- User entity does NOT know about password policy — it only stores data and provides behavioral methods

**Trade-offs:**
- Adds indirection for what could be simple string length checks
- Configuration changes require application restart (unless using `IOptionsSnapshot`)

**Date:** 2026-06-02

---

## 19. Account Lockout Strategy

**Context:** Need to prevent brute force attacks while avoiding permanent denial of service.

**Decision:** After 5 failed login attempts (`MaxFailedAccessAttempts`), account is locked for 15 minutes (`DefaultLockoutMinutes`). Returns HTTP 423 Locked during lockout period.

**Rationale:**
- 5 attempts before lockout allows for legitimate typos while preventing brute force
- 15-minute lockout is long enough to discourage attackers but short enough to not permanently block users
- HTTP 423 (Locked) provides explicit status for frontend to display meaningful error
- Lockout counter resets on successful login
- Audit log records each lockout event

**Trade-offs:**
- Attacker can intentionally lock out a user by attempting 5 failed logins (DoS)
- Mitigation: audit log detects lockout patterns; admin can manually unlock via `User.Unlock()`

**Date:** 2026-06-02

---

## 20. React Hook Form + Zod for Form Validation

**Context:** Forms need client-side validation with good UX and TypeScript integration.

**Options:**
1. React Hook Form + Zod (chosen for complex forms)
2. Plain `useState` + manual validation (chosen for simple 2-3 field forms)
3. Formik — more boilerplate, worse performance
4. HTML5 validation only — insufficient for cross-field rules

**Decision:** RHF with `zodResolver` for complex forms (user create/edit, profile edit, password change). Plain `useState` for simple forms (login, change-password page).

**Rationale:**
- RHF uses uncontrolled inputs — avoids re-renders on every keystroke
- Zod schemas define all rules (email format, min/max length, cross-field comparison) in one place
- `z.infer<typeof schema>` generates TypeScript types — no type duplication
- Login and change-password have 2-3 fields — `useState` is simpler
- Mixed approach: proportional complexity to form size

**Trade-offs:**
- Zod schemas are co-located in page files — could lead to duplication if two pages validate the same entity
- Login page doesn't benefit from RHF's error display or submission state management

**Date:** 2026-06-02
