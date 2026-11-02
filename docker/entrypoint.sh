#!/bin/sh
set -e

PORT=${PORT:-10000}

cd /app/auth && ASPNETCORE_URLS="http://0.0.0.0:5001" dotnet AuthService.dll &
cd /app/catalog && ASPNETCORE_URLS="http://0.0.0.0:5002" dotnet CatalogService.dll &
cd /app/ordering && ASPNETCORE_URLS="http://0.0.0.0:5003" dotnet OrderingService.dll &

sleep 3

cd /app/gateway && ASPNETCORE_URLS="http://0.0.0.0:${PORT}" exec dotnet ApiGateway.dll
