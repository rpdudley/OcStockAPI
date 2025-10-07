# OcStockAPI

A modern stock market data API built with .NET 8 that provides comprehensive financial data including:

## Features
- ? Stock quotes and historical data
- ? Market news and analysis  
- ? Portfolio management
- ? Mutual fund tracking
- ? Economic indicators (CPI, unemployment, interest rates)
- ? Market events and calendar
- ? Real-time background data updates
- ? Health checks and monitoring

## Tech Stack
- **.NET 8** - Latest .NET framework
- **Entity Framework Core** - ORM with PostgreSQL support
- **PostgreSQL** - Database (Supabase)
- **Swagger/OpenAPI** - API documentation
- **Background Services** - Automated data updates
- **Health Checks** - System monitoring

## API Endpoints
- `/api/stock` - Stock data and quotes
- `/api/portfolio` - Portfolio management
- `/api/news` - Market news
- `/api/events` - Market events
- `/api/tracked-stocks` - Stock tracking
- `/health` - Health check endpoint

## Getting Started

### Prerequisites
- .NET 8 SDK
- PostgreSQL database (or Supabase account)

### Configuration
Set up your user secrets for development:
```bash
dotnet user-secrets set "Database:Host" "your-db-host"
dotnet user-secrets set "Database:Port" "5432"
dotnet user-secrets set "Database:Database" "your-db-name"
dotnet user-secrets set "Database:Username" "your-username"
dotnet user-secrets set "Database:Password" "your-password"
dotnet user-secrets set "Finnhub:ApiKey" "your-finnhub-key"
dotnet user-secrets set "AlphaVantage:ApiKey" "your-alphavantage-key"
dotnet user-secrets set "NewsAPI:ApiKey" "your-newsapi-key"
```

### Database Setup
Create and apply migrations:
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Running the API
```bash
dotnet run
```

The API will be available at your configured port:
- **Swagger UI**: `http://localhost:5000/` (automatically opens)
- **Health Check**: `http://localhost:5000/health`
- **API Base**: `http://localhost:5000/api/`

## Quick Test
Once running, navigate to `http://localhost:5000` and you'll see the Swagger UI automatically load with all available endpoints ready to test!