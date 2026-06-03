# Multi-Agent Development Workflow

## Agent Roles

### 🧠 PRODUCT OWNER
- **Focus:** Requirements, priorities, business value
- **Input:** `planInicial.ia.md`, user feedback, market research
- **Output:** Feature specs, acceptance criteria, priority matrix
- **Questions:** What problem are we solving? What's the MVP scope? What's the user story?
- **Guardrails:** Does not write code. Does not design architecture.

### 🏗️ DEVELOPER
- **Focus:** Backend/frontend implementation, code quality, performance
- **Responsibilities:**
  - Entity/DTO/Controller implementation
  - Repository and service layer
  - Database migrations and EF Core configuration
  - API contracts and response formats
  - State management (Zustand) and API integration (Axios)
  - CQRS with MediatR pipeline behaviors
  - Form validation with FluentValidation + Zod
- **Key patterns:**
  - Hexagonal architecture: Domain → Application → Infrastructure → WebApi
  - Repository pattern with UnitOfWork
  - Soft-delete with global query filters
  - JWT access + refresh token rotation
  - Rate limiting with named policies
- **Files:** `src/backend/`, `src/frontend/src/stores/`, `src/frontend/src/lib/`

### 🎨 UX/UI
- **Focus:** Component architecture, design system, accessibility, responsive
- **Responsibilities:**
  - shadcn/ui component selection and customization
  - Tailwind v4 CSS architecture (OKLCH tokens, `@theme`)
  - Layout shell (sidebar, header, responsive breakpoints)
  - Form UX (RHF + Zod error display, loading states)
  - Session warning modal countdown UX
  - Dark/light mode (future)
- **Key patterns:**
  - `cn()` utility with `clsx` + CVA
  - `@base-ui/react` primitives for accessible components
  - Collapsible sidebar with localStorage persistence
  - Consistent spacing, color tokens, typography scale
- **Files:** `src/frontend/src/components/`, `src/frontend/src/index.css`

### 🧪 QA
- **Focus:** Test coverage, edge cases, regression prevention
- **Responsibilities:**
  - Unit tests (xUnit + Moq + FluentAssertions)
  - Integration tests with Testcontainers (future)
  - Frontend tests with Vitest + Testing Library (future)
  - Test project structure mirroring source
  - Code coverage with coverlet
- **Key patterns:**
  - Repository mocking with `Mock<IUnitOfWork>`
  - Controller tests with mocked `HttpContext` and `ClaimsPrincipal`
  - Entity behavior tests for domain invariants
  - Validator tests for every FluentValidation rule
- **Files:** `tests/`

### 🔒 SECURITY AUDIT
- **Focus:** Authentication, authorization, data protection, OWASP
- **Responsibilities:**
  - JWT configuration (HS512, short-lived access, refresh rotation)
  - Rate limiting policies (Login: 10/min, ForgotPassword: 3/hr)
  - Account lockout (5 failed attempts → 15 min lock)
  - Password policy (length, complexity, expiration)
  - Security headers middleware (CSP, X-Frame-Options, etc.)
  - Audit logging for critical operations
  - Anti-enumeration (same message for invalid email vs wrong password)
  - SQL injection prevention (EF Core parameterized queries)
  - CSRF protection via JWT (not separate anti-forgery token)
- **Key patterns:**
  - `[EnableRateLimiting("Login")]` on auth endpoints
  - IP + User-Agent capture in audit logs
  - Refresh token hashing with SHA-256 + `FixedTimeEquals`
  - Permission-based authorization via JWT claims
- **Files:** `src/backend/UserManagement.WebApi/Middleware/`, `src/backend/UserManagement.WebApi/Controllers/AuthController.cs`

### 🚀 DEVOPS
- **Focus:** CI/CD, Docker, deployment, monitoring, secrets
- **Responsibilities:**
  - Docker multi-stage builds (SDK → runtime, Node → nginx)
  - Docker Compose topology (Traefik + PostgreSQL + backend + frontend)
  - Nginx SPA routing + asset caching
  - Traefik automatic TLS via Let's Encrypt
  - Environment variable management (.env template)
  - Serilog sinks (Console + File, future: PostgreSQL, Prometheus)
  - Health checks (`/health`, `/health/ready`)
  - `.gitignore` maintenance
- **Key patterns:**
  - `condition: service_healthy` for database dependency
  - Label-based Traefik routing
  - Alpine-based images for minimal attack surface
- **Files:** `src/docker/`, `.env.template`, `docker-compose*.yml`

## Workflow Phases

### Phase 1: Exploration (`/opsx-explore`)
```
[PRODUCT OWNER] defines problem + scope
       ↓
[SECURITY AUDIT] identifies risks + compliance needs
       ↓
[DEVELOPER] + [UX/UI] research technical feasibility
       ↓
[QA] identifies test scenarios
       ↓
[DEVOPS] assesses infrastructure impact
```

### Phase 2: Proposal (`/opsx-propose`)
```
[PRODUCT OWNER] → proposal.md (what + why)
[DEVELOPER] → design.md (architecture + decisions)
[QA] → tasks.md (test plan)
All → .openspec.yaml
```

### Phase 3: Implementation (`/opsx-apply`)
```
For each task in tasks.md:
  1. [DEVELOPER] implements
  2. [DEVELOPER] adds technical comments (decision rationale)
  3. [QA] verifies with existing tests + adds new tests
  4. [SECURITY AUDIT] reviews for vulnerabilities
  5. [UX/UI] reviews frontend implementation
  6. [DEVOPS] ensures docker/deploy compatibility
  7. ALL: task marked - [x] in tasks.md
```

### Phase 4: Archive (`/opsx-archive`)
```
[QA] runs full test suite → report
[SECURITY AUDIT] signs off
[DEVOPS] deploys to staging
[PRODUCT OWNER] accepts
Move to archive/
```

## Communication Protocol

| Trigger | Action |
|---------|--------|
| Ambiguous requirement | Escalate to PRODUCT OWNER via `question` tool |
| Security concern | Pause, engage SECURITY AUDIT |
| Breaking test | Pause, engage QA + DEVELOPER |
| Infrastructure change | Notify DEVOPS for docker/deploy review |
| UI/UX decision | Consult UX/UI before implementing |
| Cross-cutting change | All agents review in sequence before commit |

## Decision Log Format

Every significant decision must be documented with:
- **Context:** What prompted this decision
- **Options:** Alternatives considered
- **Decision:** What was chosen
- **Rationale:** Why this option over others
- **Trade-offs:** What was sacrificed
- **Date:** When the decision was made
