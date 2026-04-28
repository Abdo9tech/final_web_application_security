#!/usr/bin/env bash
set -euo pipefail

DB_HOST="${DB_HOST:-mssql}"
DB_PORT="${DB_PORT:-1433}"
DB_NAME="${SQL_DATABASE:-BookifyHotelDB}"
DB_USER="${SQL_USER:-sa}"
DB_PASSWORD="${SQL_PASSWORD:?SQL_PASSWORD is required}"
PROJECT_PATH="${PROJECT_PATH:-Project(DEPI)/HotelEcomm.csproj}"
MAX_RETRIES="${MAX_RETRIES:-60}"
SLEEP_SECONDS="${SLEEP_SECONDS:-2}"

echo "Waiting for SQL Server at ${DB_HOST}:${DB_PORT}..."
for ((i=1; i<=MAX_RETRIES; i++)); do
  if /opt/mssql-tools18/bin/sqlcmd -S "${DB_HOST},${DB_PORT}" -U "${DB_USER}" -P "${DB_PASSWORD}" -Q "SELECT 1" -C >/dev/null 2>&1; then
    echo "SQL Server is ready."
    break
  fi

  if [[ "$i" -eq "$MAX_RETRIES" ]]; then
    echo "Timed out waiting for SQL Server."
    exit 1
  fi

  sleep "${SLEEP_SECONDS}"
done

echo "Ensuring database exists: ${DB_NAME}"
/opt/mssql-tools18/bin/sqlcmd -S "${DB_HOST},${DB_PORT}" -U "${DB_USER}" -P "${DB_PASSWORD}" -Q "IF DB_ID('${DB_NAME}') IS NULL CREATE DATABASE [${DB_NAME}]" -C

echo "Running Entity Framework migrations..."
dotnet tool restore
dotnet ef database update --project "${PROJECT_PATH}" --startup-project "${PROJECT_PATH}" --no-build

echo "Database initialization complete."
