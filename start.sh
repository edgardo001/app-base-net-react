#!/bin/bash
set -e

cd "$(dirname "$0")"

echo "========================================"
echo "  User Management Platform - Starting..."
echo "========================================"
echo ""

if docker ps --filter "name=mvp-postgres" --format "{{.Names}}" | grep -q "mvp-postgres" 2>/dev/null; then
    echo "[1/3] PostgreSQL already running."
else
    echo "[1/3] Starting PostgreSQL 18..."
    docker run -d --name mvp-postgres \
        -e POSTGRES_DB=mvp-usuarios-db \
        -e POSTGRES_USER=mvp-usuarios-db \
        -e POSTGRES_PASSWORD=mvp-usuarios-dev-2024 \
        -p 5432:5432 \
        postgres:18-alpine >/dev/null 2>&1
    echo "      PostgreSQL started."
fi

echo "[2/3] Starting Backend (http://localhost:5011)..."
if command -v gnome-terminal &>/dev/null; then
    gnome-terminal --title="Backend" -- bash -c "dotnet run --project src/backend/UserManagement.WebApi --launch-profile http; exec bash"
elif command -v xterm &>/dev/null; then
    xterm -T "Backend" -e "dotnet run --project src/backend/UserManagement.WebApi --launch-profile http" &
elif command -v konsole &>/dev/null; then
    konsole --new-tab -p tab-title="Backend" -e "dotnet run --project src/backend/UserManagement.WebApi --launch-profile http" &
else
    echo "      WARNING: Starting backend in background (logs in backend.log)..."
    dotnet run --project src/backend/UserManagement.WebApi --launch-profile http > backend.log 2>&1 &
fi

sleep 5

echo "[3/3] Starting Frontend (http://localhost:5173)..."
if command -v gnome-terminal &>/dev/null; then
    gnome-terminal --title="Frontend" -- bash -c "cd src/frontend && npm run dev; exec bash"
elif command -v xterm &>/dev/null; then
    xterm -T "Frontend" -e "cd src/frontend && npm run dev" &
elif command -v konsole &>/dev/null; then
    konsole --new-tab -p tab-title="Frontend" -e "cd src/frontend && npm run dev" &
else
    echo "      WARNING: Starting frontend in background (logs in frontend.log)..."
    cd src/frontend && npm run dev > ../../frontend.log 2>&1 &
    cd "$(dirname "$0")"
fi

echo ""
echo "========================================"
echo "  All services starting!"
echo ""
echo "  Backend:   http://localhost:5011"
echo "  Scalar UI: http://localhost:5011/scalar/v1"
echo "  Frontend:  http://localhost:5173"
echo ""
echo "  Login: admin"
  echo "  Pass:  admin"
echo "========================================"
