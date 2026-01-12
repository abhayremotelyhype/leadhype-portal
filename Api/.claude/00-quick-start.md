# LeadHype API - Quick Start

**READ THIS FIRST IN EVERY NEW SESSION**

## Project Identity
- **Name**: LeadHype API (formerly Smartlead-related)
- **Assembly**: LeadHype.Api
- **Type**: ASP.NET Core Web API
- **Framework**: .NET 9.0
- **Database**: PostgreSQL (leadhype_db)

## Tech Stack
- **Runtime**: .NET 9.0
- **Web**: ASP.NET Core (Kestrel server on port 5010)
- **Database**: PostgreSQL with Dapper ORM
- **Auth**: JWT Bearer + API Key authentication
- **API Docs**: Scalar + OpenAPI/Swagger
- **Background Jobs**: IHostedService
- **Password Hashing**: BCrypt.Net
- **Web Scraping**: Selenium WebDriver + HtmlAgilityPack
- **CSV**: CsvHelper
- **JSON**: Newtonsoft.Json + System.Text.Json

## Core Domain
Lead and email campaign management platform with Smartlead integration:
- **Campaigns**: Email sequences, templates, lead tracking
- **Email Accounts**: Warmup tracking, daily stats, Google OAuth
- **Leads**: Conversations, email history, reply classification
- **Clients**: Multi-tenant client management
- **Webhooks**: Event-driven notifications
- **Analytics**: Dashboard stats, daily metrics

## How to Run
```bash
# Build
dotnet build

# Run (Development)
dotnet run

# Run (Production - specific port)
ASPNETCORE_URLS="http://localhost:5010" dotnet run

# Run without rebuild
ASPNETCORE_URLS="http://localhost:5010" dotnet run --no-build
```

## Database Connection
- **Host**: localhost:5432
- **Database**: leadhype_db
- **Connection Pooling**: Min 5, Max 20 connections
- **Migrations**: Automatic on startup via DatabaseMigrationService

## Project Structure
```
Api/
├── Program.cs                    # Entry point, DI setup, middleware pipeline
├── Core/
│   ├── Controllers/             # 18 API controllers (REST endpoints)
│   ├── Services/                # 20 business logic services
│   ├── Database/
│   │   ├── Repositories/        # 32 data access repositories (Dapper)
│   │   ├── Migrations/          # SQL migration files
│   │   └── PostgreSqlConnectionService.cs
│   ├── Models/
│   │   ├── Database/            # Entity models (Campaign, Client, User, etc.)
│   │   ├── DTOs/                # Data transfer objects
│   │   ├── API/                 # Request/response models
│   │   ├── Frontend/            # Frontend-specific models
│   │   └── ExternalApi/         # Smartlead/external API models
│   ├── Middleware/              # Request pipeline middleware
│   ├── Authentication/          # JWT + API Key handlers
│   ├── Security/                # Security utilities
│   ├── Managers/                # TaskManager, MultiLogin
│   └── Extensions/              # Extension methods
├── frontend/                     # Next.js frontend (separate)
└── .claude/                      # THIS DOCUMENTATION
```

## Key Conventions
- **Async/Await**: All I/O operations are async
- **Repository Pattern**: Controllers → Services → Repositories → Database
- **Scoped Services**: Most services are scoped per request
- **Singleton**: DbConnectionService (connection pooling), JwtSettings, RateLimitService
- **DI Registration**: Program.cs lines 50-115
- **Naming**:
  - Interfaces: `I{Name}` (e.g., ICampaignRepository)
  - Implementations: `{Name}` (e.g., CampaignRepository)
  - V1 APIs: `{Name}V1Controller` for versioned endpoints

## Authentication
1. **JWT**: Bearer token in Authorization header
   - Expiration: 43200 minutes (~30 days)
   - Refresh token: 90 days
2. **API Key**: X-API-Key header for service-to-service
3. **Sessions**: UserSession table tracks active sessions
4. **Rate Limiting**: RateLimitService prevents abuse

## External Integrations
- **Smartlead API**: Lead sync, campaign data fetching
- **RevReply API**: Email reply classification
- **Multilogin**: Browser automation (token in appsettings.json)
- **Google OAuth**: Email account connection

## Background Services
- **SessionCleanupService**: Removes expired sessions
- **WebhookEventMonitoringService**: Monitors campaign metrics, triggers webhooks

## Development Notes
- **CORS**: Configured for localhost:3000, localhost:5010, production domain
- **Migrations**: Run automatically on startup (see Program.cs:186-191)
- **Admin Init**: Default admin user created on first run
- **API Explorer**: http://localhost:5010/scalar/v1 (Scalar docs)
- **Request Logging**: Enabled in Development environment only

## Important Files to Reference
- [Program.cs](../Program.cs) - Complete DI and middleware setup
- [appsettings.json](../appsettings.json) - Configuration (JWT, DB, API keys)
- See other .claude/*.md files for detailed documentation

## Next Steps
- For API endpoints: See [02-controllers-endpoints.md](02-controllers-endpoints.md)
- For database schema: See [04-database-schema.md](04-database-schema.md)
- For business logic: See [07-services.md](07-services.md)
