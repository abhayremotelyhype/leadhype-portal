# LeadHype API Documentation

**Complete documentation for LeadHype ASP.NET Core API**

This directory contains comprehensive documentation to help you (and Claude Code) quickly understand the project structure in future sessions.

## 📖 Documentation Files

### Getting Started
- **[00-quick-start.md](00-quick-start.md)** ⭐ **START HERE**
  - Project overview
  - Tech stack (.NET 9, PostgreSQL, JWT)
  - How to run
  - Project structure
  - Key conventions

### Architecture & Design
- **[01-architecture.md](01-architecture.md)**
  - Layered architecture
  - Dependency injection
  - Middleware pipeline
  - Startup sequence
  - Background services

### API Layer
- **[02-controllers-endpoints.md](02-controllers-endpoints.md)**
  - All 19 controllers
  - 200+ API endpoints
  - Authentication requirements
  - Request/response formats
  - Role-based access

- **[03-authentication.md](03-authentication.md)**
  - JWT authentication flow
  - API Key authentication
  - Session management
  - Rate limiting
  - User roles and permissions
  - Security features

### Data Layer
- **[04-database-schema.md](04-database-schema.md)**
  - PostgreSQL schema (24+ tables)
  - Table relationships
  - Indexes and constraints
  - Common queries
  - Multi-tenancy patterns

- **[05-repositories.md](05-repositories.md)**
  - All 32 repositories
  - Dapper patterns
  - Connection management
  - Transaction support

- **[06-migrations.md](06-migrations.md)**
  - Migration system
  - Migration files (001-003)
  - Auto-update triggers
  - Creating new migrations

### Business Logic
- **[07-services.md](07-services.md)**
  - All 20+ services
  - Service categories
  - Dependencies
  - Key methods
  - Service interconnections

- **[08-smartlead-integration.md](08-smartlead-integration.md)**
  - Smartlead API integration
  - Sync service (2-hour loop)
  - RevReply AI classification
  - Event sourcing
  - Resumable syncs

### Code Organization
- **[09-models-structure.md](09-models-structure.md)**
  - Model categories (Database, API, DTOs, etc.)
  - Common patterns
  - JSONB handling
  - Validation

- **[10-middleware-security.md](10-middleware-security.md)**
  - Middleware pipeline
  - CORS configuration
  - Security headers
  - JWT validation
  - API key middleware

- **[11-dependencies.md](11-dependencies.md)**
  - NuGet packages
  - Key features by package
  - Version info

### Patterns & Best Practices
- **[12-common-patterns.md](12-common-patterns.md)**
  - Async/await
  - Repository pattern
  - Pagination
  - UPSERT
  - Error handling
  - JSONB operations
  - JWT claims

- **[13-tasks-background-jobs.md](13-tasks-background-jobs.md)**
  - TaskManager (OAuth automation)
  - MultiLogin integration
  - Background services (cleanup, monitoring)
  - Startup tasks (Smartlead sync)

## 🚀 Quick Reference

### For New Features
1. Read [00-quick-start.md](00-quick-start.md) for overview
2. Check [02-controllers-endpoints.md](02-controllers-endpoints.md) for existing endpoints
3. Review [07-services.md](07-services.md) for business logic
4. Reference [04-database-schema.md](04-database-schema.md) for data model

### For Bug Fixes
1. Find endpoint in [02-controllers-endpoints.md](02-controllers-endpoints.md)
2. Trace service in [07-services.md](07-services.md)
3. Check repository in [05-repositories.md](05-repositories.md)
4. Review schema in [04-database-schema.md](04-database-schema.md)

### For Authentication Issues
1. Read [03-authentication.md](03-authentication.md)
2. Check [10-middleware-security.md](10-middleware-security.md)

### For Database Changes
1. Review [04-database-schema.md](04-database-schema.md)
2. Create migration following [06-migrations.md](06-migrations.md)
3. Update repository in [05-repositories.md](05-repositories.md)

## 🎯 Key Concepts

### Multi-tenancy
All data filtered by `admin_uuid` - always include in queries

### Event Sourcing
Campaign statistics use immutable `campaign_events` table for time-range filtering

### Dual Authentication
- **JWT**: Web clients (access + refresh tokens)
- **API Keys**: External integrations with permissions

### Background Sync
SmartleadSyncService runs every 2 hours syncing from Smartlead API

### Connection Pooling
Singleton IDbConnectionService with 5-20 PostgreSQL connections

## 📊 Project Stats

- **Controllers**: 19
- **Endpoints**: 200+
- **Services**: 20+
- **Repositories**: 32
- **Database Tables**: 24+
- **Migrations**: 3 (+ 1 C# migration)
- **Lines of Code**: ~15,000+ (C#)

## 🔍 Finding Things

### By Feature Area
- **Campaigns**: Controllers/Campaign/, Services/Campaign/
- **Email Accounts**: Controllers/Campaign/EmailAccountController.cs
- **Analytics**: Controllers/Analytics/, Services/Analytics/
- **Authentication**: Controllers/Authentication/, Services/Authentication/
- **Webhooks**: Controllers/ExternalApi/

### By Concern
- **Database**: Core/Database/
- **Models**: Core/Models/
- **Middleware**: Core/Middleware/
- **DI Setup**: Program.cs
- **Configuration**: appsettings.json

## 🛠️ Development Workflow

1. **Read relevant docs** from this directory
2. **Make changes** to code
3. **Run migrations** if schema changed
4. **Test locally** with `dotnet run`
5. **Update docs** if architecture changed

## 💡 Tips for Claude Code Sessions

**Always start new sessions by reading:**
1. [00-quick-start.md](00-quick-start.md) - Project context
2. Relevant specific docs for your task

**Don't read:**
- bin/, obj/, node_modules/ - excluded via .claudeignore
- Media files, logs, databases - excluded via .claudeignore

## 📝 Maintenance

**When to update docs:**
- New controllers/endpoints → Update 02
- New services → Update 07
- Schema changes → Update 04, 06
- New dependencies → Update 11
- New patterns → Update 12

## ⚡ Performance Notes

- Use batch operations for multiple entities
- Filter by admin_uuid for multi-tenancy
- Leverage indexes (see 04-database-schema.md)
- Connection pooling handles concurrency
- Event sourcing enables fast time-range queries

## 🔐 Security Checklist

- ✅ BCrypt password hashing
- ✅ JWT with 32+ char secret
- ✅ API key SHA256 hashing
- ✅ CORS configured
- ✅ Security headers
- ✅ Rate limiting
- ⚠️ HTTPS redirect (disabled, enable for production)
- ⚠️ Session validation (disabled)

## 📞 Support

For questions about:
- **Architecture**: See 01-architecture.md
- **Authentication**: See 03-authentication.md
- **Database**: See 04-database-schema.md
- **Services**: See 07-services.md

---

**Last Updated**: 2025-10-06
**Documentation Version**: 1.0
**API Version**: LeadHype API v1
