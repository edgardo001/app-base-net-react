# User Management Platform — Design Document

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    WebApi (ASP.NET Core 10)                  │
│  ┌─────────┐ ┌──────────┐ ┌───────────┐ ┌───────────────┐  │
│  │ Auth    │ │ Users    │ │ Roles     │ │ Admin         │  │
│  │Controller│ │Controller│ │Controller │ │Controller     │  │
│  └────┬────┘ └────┬─────┘ └─────┬─────┘ └──────┬────────┘  │
│       │           │             │              │            │
│  ┌────┴───────────┴─────────────┴──────────────┴────────┐   │
│  │              MediatR (CQRS)                           │   │
│  │  Commands → Handlers → Validators → AutoMapper        │   │
│  └───────────────────────┬──────────────────────────────┘   │
└──────────────────────────┼──────────────────────────────────┘
                           │
┌──────────────────────────┼──────────────────────────────────┐
│              Application Layer                               │
│  ┌───────────────────────┴──────────────────────────────┐   │
│  │  Interfaces: IUserRepository, IJwtService, etc.      │   │
│  └───────────────────────┬──────────────────────────────┘   │
└──────────────────────────┼──────────────────────────────────┘
                           │
┌──────────────────────────┼──────────────────────────────────┐
│              Infrastructure Layer                            │
│  ┌───────────┐  ┌───────────┐  ┌────────┐  ┌───────────┐  │
│  │ EF Core   │  │ JWT       │  │ MailKit│  │ Quartz.NET│  │
│  │ PostgreSQL│  │ Service   │  │ Email  │  │ Jobs      │  │
│  └───────────┘  └───────────┘  └────────┘  └───────────┘  │
└──────────────────────────┼──────────────────────────────────┘
                           │
┌──────────────────────────┼──────────────────────────────────┐
│              Domain Layer                                    │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  Entities: User, Role, Permission, RefreshToken,     │   │
│  │            AuditLog, LoginAttempt                     │   │
│  │  Common: BaseEntity                                   │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

## Authentication Flow (JWT)

```
┌────────┐     ┌──────────┐     ┌──────────┐     ┌──────────┐
│ Client │────▶│  Login   │────▶│ Validate │────▶│ Generate │
│        │     │ Endpoint │     │Password  │     │ Tokens   │
└────────┘     └──────────┘     └──────────┘     └────┬─────┘
                                                      │
                                              ┌───────┴───────┐
                                              │ Access Token   │
                                              │ (15 min) +     │
                                              │ Refresh Token  │
                                              │ (7 days)       │
                                              └───────┬───────┘
                                                      │
                                              ┌───────┴───────┐
                                              │ Store Refresh  │
                                              │ Token in DB    │
                                              │ (hashed)       │
                                              └───────────────┘
```

## Refresh Token Rotation

```
Initial Login: Issue RT#1
    ↓
Refresh with RT#1:
    ├── Revoke RT#1
    ├── Issue RT#2
    └── Replace RT#1 metadata with RT#2 hash
    ↓
Refresh with RT#1 (stolen/reused):
    ├── Revoke ALL tokens for user
    ├── Log security event
    └── Notify user
```

## Data Model (ERD)

```
User ──────┐  ┌── Role
  │         │  │   │
  │   UserRole  │   │
  │         │   │   │
  │         └───┘   │
  │              RolePermission
  │                 │
  │                 └── Permission
  │
  ├── RefreshToken (1:N)
  ├── AuditLog (1:N)
  └── LoginAttempt (1:N)
```

## Seed Data

### Admin User
- Email: `admin`
- Password: `admin` (hashed with Argon2id)
- Role: SuperAdmin

### Default Roles
| Role | Type | Permissions |
|------|------|-------------|
| SuperAdmin | System | All |
| Admin | System | users:*, roles:*, permissions:*, audit:*, admin:* |
| user-tipo-a | Dynamic | page-a:view, profile:own:* |
| user-tipo-b | Dynamic | page-b:view, profile:own:* |
| user-tipo-c | Dynamic | page-c:view, profile:own:* |

## API Endpoints
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | /api/auth/login | No | Login with email/password + Turnstile |
| POST | /api/auth/refresh | No | Refresh JWT tokens |
| POST | /api/auth/logout | Yes | Revoke refresh token |
| POST | /api/auth/forgot-password | No | Request password reset |
| POST | /api/auth/reset-password | No | Reset with temp password |
| POST | /api/auth/change-password | Yes | Change password |
| POST | /api/auth/confirm-email | No | Confirm email address |
| GET | /api/users | Admin | List users (paged) |
| POST | /api/users | Admin | Create user |
| PUT | /api/users/{id} | Admin | Update user |
| DELETE | /api/users/{id} | Admin | Soft delete user |
| PATCH | /api/users/{id}/activate | Admin | Toggle active |
| PATCH | /api/users/{id}/reset-password | Admin | Reset user password |
| PATCH | /api/users/{id}/revoke-tokens | Admin | Revoke user sessions |
| POST | /api/users/{id}/avatar | Yes | Upload avatar |
| GET | /api/profile | Yes | Get own profile |
| PUT | /api/profile | Yes | Update own profile |
| PUT | /api/profile/avatar | Yes | Update own avatar |
| GET | /api/profile/activity | Yes | Own activity log |
| GET | /api/roles | Admin | List roles |
| POST | /api/roles | Admin | Create role |
| PUT | /api/roles/{id} | Admin | Update role |
| DELETE | /api/roles/{id} | Admin | Delete role |
| PATCH | /api/roles/{id}/permissions | Admin | Assign permissions |
| GET | /api/permissions | Admin | List permissions |
| GET | /api/permissions/modules | Admin | Permissions by module |
| GET | /api/admin/dashboard | Admin | Dashboard metrics |
| GET | /api/admin/audit-log | Admin | Audit log |
| POST | /api/admin/revoke-all-tokens | SuperAdmin | Global token revoke |
| GET | /health | No | Liveness |
| GET | /health/ready | No | Readiness |

## Security Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| JWT Algorithm | HS512 | Simple, fast, no public key distribution needed |
| Password Hash | Argon2id | Memory-hard, GPU-resistant |
| Refresh Token | Hashed in DB | Prevents DB read → token theft |
| Token Transport | Bearer header | Standard, no cookies (avoids CSRF) |
| Rate Limiting | Fixed window | Simple, built into .NET 10 |
| Captcha | Cloudflare Turnstile | Privacy-friendly, conditional |
| API Docs | Scalar UI | Modern, clean, MIT license |

## Frontend Component Tree

```
App
├── AuthLayout
│   ├── LoginPage
│   ├── ForgotPasswordPage
│   └── ResetPasswordPage
├── MainLayout
│   ├── Navbar (user menu, theme toggle, notifications)
│   ├── Sidebar (navigation links by role/permissions)
│   └── ContentArea
│       ├── DashboardPage (Admin)
│       ├── UsersPage (Admin)
│       │   ├── UserDataTable
│       │   ├── CreateUserModal
│       │   └── EditUserModal
│       ├── RolesPage (Admin)
│       │   ├── RoleDataTable
│       │   ├── CreateRoleModal
│       │   └── RolePermissionsModal
│       ├── PermissionsPage (Admin)
│       ├── ProfilePage (All)
│       ├── PageA (user-tipo-a)
│       ├── PageB (user-tipo-b)
│       └── PageC (user-tipo-c)
└── SessionExpiredModal
```

## State Management (Zustand)

```
authStore
├── user: User | null
├── accessToken: string | null
├── refreshToken: string | null
├── permissions: string[]
├── isAuthenticated: boolean
├── sessionExpiringAt: number | null
├── login(email, password): Promise<void>
├── logout(): void
├── refreshSession(): Promise<void>
├── hasPermission(code: string): boolean
└── hasRole(role: string): boolean

uiStore
├── sidebarOpen: boolean
├── theme: 'light' | 'dark'
└── toggleTheme(): void
```
