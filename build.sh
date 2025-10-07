# Render Build Script
#!/usr/bin/env bash
set -e

echo "Starting .NET build for OcStockAPI..."
dotnet restore OcStockAPI.sln
dotnet publish OcStockAPI/OcStockAPI.csproj -c Release -o ./publish --no-restore

echo "OcStockAPI build completed successfully!"