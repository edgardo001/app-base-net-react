@echo off
title User Management Platform
cd /d "%~dp0"

echo ========================================
echo   User Management Platform - Starting...
echo ========================================
echo.

docker ps --filter "name=mvp-postgres" --format "{{.Names}}" | findstr "mvp-postgres" >nul 2>&1
if %errorlevel% neq 0 (
    echo [1/3] Starting PostgreSQL 18...
    docker run -d --name mvp-postgres ^
        -e POSTGRES_DB=mvp-usuarios-db ^
        -e POSTGRES_USER=mvp-usuarios-db ^
        -e POSTGRES_PASSWORD=mvp-usuarios-dev-2024 ^
        -p 5432:5432 ^
        postgres:18-alpine >nul 2>&1
    echo       PostgreSQL started.
) else (
    echo [1/3] PostgreSQL already running.
)

echo [2/3] Starting Backend (http://localhost:5011)...
start "Backend" cmd /k "dotnet run --project src\backend\UserManagement.WebApi --launch-profile http"

timeout /t 8 /nobreak >nul

echo [3/3] Starting Frontend (http://localhost:5173)...
start "Frontend" cmd /k "cd /d "%~dp0src\frontend" && npm run dev"

echo.
echo ========================================
echo   All services starting!
echo.
echo   Backend:   http://localhost:5011
echo   Scalar UI: http://localhost:5011/scalar/v1
echo   Frontend:  http://localhost:5173
echo.
echo   Login: admin
echo   Pass:  admin
echo ========================================
echo.
pause
