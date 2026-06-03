## ADDED Requirements

### Requirement: Multi-stage Docker builds
The system SHALL provide Dockerfiles for backend and frontend with multi-stage builds for optimized image sizes.

#### Scenario: Backend Docker build
- **WHEN** Dockerfile.backend is built
- **THEN** Stage 1 SHALL use `mcr.microsoft.com/dotnet/sdk:10.0` to restore and publish
- **THEN** Stage 2 SHALL use `mcr.microsoft.com/dotnet/aspnet:10.0` with only published output
- **THEN** exposed port SHALL be 8080

#### Scenario: Frontend Docker build
- **WHEN** Dockerfile.frontend is built
- **THEN** Stage 1 SHALL use `node:22-alpine` to install dependencies and build
- **THEN** Stage 2 SHALL use `nginx:1.27-alpine` serving compiled assets

### Requirement: Docker Compose orchestration
The system SHALL provide docker-compose.yml with PostgreSQL, backend, frontend, and Traefik services.

#### Scenario: Infrastructure services
- **WHEN** docker-compose up is executed
- **THEN** PostgreSQL 18 Alpine SHALL start with configured database, user, and password
- **THEN** backend SHALL start after PostgreSQL health check passes
- **THEN** frontend SHALL serve the SPA via nginx proxying API calls to backend

### Requirement: Frontend SPA routing with nginx
The SPA SHALL be served via nginx with proper fallback to index.html for client-side routing.

#### Scenario: SPA route fallback
- **WHEN** a request is made to a client-side route (e.g., /users)
- **THEN** nginx SHALL serve index.html if the file does not exist

#### Scenario: API proxy
- **WHEN** a request is made to /api/* path
- **THEN** nginx SHALL proxy the request to the backend service

#### Scenario: Static asset caching
- **WHEN** requests are made for .js, .css, .png, .jpg, .webp files
- **THEN** nginx SHALL set Cache-Control: public, immutable with 1-year expiry
