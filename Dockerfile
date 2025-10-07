#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["OcStockAPI/OcStockAPI.csproj", "OcStockAPI/"]
RUN dotnet restore "OcStockAPI/OcStockAPI.csproj"

# Copy source code and build
COPY OcStockAPI/ OcStockAPI/
WORKDIR "/src/OcStockAPI"
RUN dotnet build "OcStockAPI.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "OcStockAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published application
COPY --from=publish /app/publish .

# Render uses PORT environment variable
ENV ASPNETCORE_URLS=http://0.0.0.0:$PORT
EXPOSE $PORT

ENTRYPOINT ["dotnet", "OcStockAPI.dll"]