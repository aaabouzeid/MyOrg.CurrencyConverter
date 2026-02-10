#!/bin/bash
set -e

echo "Starting Currency Converter API..."

# Wait for PostgreSQL to be ready
echo "Waiting for PostgreSQL to be ready..."
until dotnet ef database update --no-build 2>/dev/null || [ $? -eq 1 ]; do
  echo "PostgreSQL is unavailable - sleeping"
  sleep 2
done

echo "PostgreSQL is up - applying migrations"

# Apply database migrations
dotnet ef database update --no-build

echo "Migrations applied successfully"

# Start the application
echo "Starting application..."
exec dotnet MyOrg.CurrencyConverter.API.dll
