# Models Structure

**Location:** [Core/Models/](../Core/Models/)

## Model Categories

### Database/ - Entity Models
[Core/Models/Database/](../Core/Models/Database/)

Maps directly to database tables:

**Campaign/**
- CampaignDetailsDbModel.cs
- CampaignDailyStatEntry.cs
- EmailTemplateDbModel.cs
- LeadConversationDbModel.cs
- LeadEmailHistoryDbModel.cs
- PositiveReplyDbModel.cs
- ClassifiedEmailDbModel.cs
- CampaignSyncProgressDbModel.cs

**EmailAccount/**
- EmailAccount.cs
- EmailAccountDailyStatEntry.cs
- EmailAccountStatsDate.cs
- WarmupDetails.cs
- WarmupMetricsDto.cs
- WarmupDailyDataDto.cs

**General/**
- Client.cs
- Setting.cs

**User/**
- User.cs
- UserSession.cs

**WebhookEvent/**
- WebhookEventConfig.cs

### DTOs/ - Data Transfer Objects
[Core/Models/DTOs/](../Core/Models/DTOs/)

For internal data transfer between layers

### API/ - Request/Response Models
[Core/Models/API/](../Core/Models/API/)

**Requests/**
- Campaign creation, update, filtering
- Lead upload
- Webhook configuration
- User management

**Responses/**
- Paginated responses
- API responses with success/error
- Statistics responses

**Smartlead/**
- External API response models
- Smartlead-specific DTOs

### Frontend/ - UI Models
[Core/Models/Frontend/](../Core/Models/Frontend/)

Models specifically for frontend consumption

### Auth/ - Authentication Models
[Core/Models/Auth/](../Core/Models/Auth/)

- User.cs
- UserSession.cs
- JwtSettings.cs
- LoginRequest.cs
- LoginResponse.cs
- RefreshTokenRequest.cs
- CreateUserRequest.cs
- UpdateUserRequest.cs
- UserInfo.cs
- UserRoles.cs (static class)

### Security/ - Security Models
[Core/Models/Security/](../Core/Models/Security/)

- ApiKey.cs
- ApiKeyPermissions.cs (static class)
- ApiKeyUsage.cs

### Shared/ - Cross-cutting Models
[Core/Models/Shared/](../Core/Models/Shared/)

Common models used across multiple areas

### ExternalApi/ - External API Models
[Core/Models/ExternalApi/](../Core/Models/ExternalApi/)

Models for external API integrations (Smartlead, RevReply, etc.)

## Common Model Patterns

### Base Properties
Most database models have:
```csharp
public string Id { get; set; }           // UUID
public DateTime CreatedAt { get; set; }
public DateTime UpdatedAt { get; set; }
```

### Multi-tenancy
Many models include:
```csharp
public string AdminUuid { get; set; }  // Tenant identifier
```

### JSONB Fields
Models with JSON columns:
```csharp
public List<string> AssignedClientIds { get; set; }  // From JSONB
public List<string> Tags { get; set; }               // From JSONB
public List<int> EmailIds { get; set; }              // From JSONB
public Dictionary<string, string> Headers { get; set; } // From JSONB
```

### Nullable References
.NET 9.0 with nullable reference types enabled:
```csharp
public string Name { get; set; } = null!;  // Required
public string? Description { get; set; }    // Optional
```

## Key Models

### User (Auth/)
```csharp
public class User
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public string Role { get; set; }  // "Admin" or "User"
    public bool IsActive { get; set; }
    public List<string> AssignedClientIds { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
```

### Campaign (Database/Campaign/)
```csharp
public class CampaignDetailsDbModel
{
    public string Id { get; set; }          // UUID
    public int CampaignId { get; set; }     // Smartlead ID
    public string Name { get; set; }
    public string? ClientId { get; set; }
    public string Status { get; set; }
    public int TotalSent { get; set; }
    public int TotalOpened { get; set; }
    public int TotalReplied { get; set; }
    public List<int> EmailIds { get; set; }
    public List<string> Tags { get; set; }
}
```

### ApiKey (Security/)
```csharp
public class ApiKey
{
    public string Id { get; set; }
    public string KeyHash { get; set; }       // SHA256
    public string KeyPrefix { get; set; }     // First 8 chars
    public string UserId { get; set; }
    public List<string> Permissions { get; set; }
    public int RateLimit { get; set; }
    public List<string> IpWhitelist { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
```

### PaginatedResponse<T> (API/)
```csharp
public class PaginatedResponse<T>
{
    public List<T> Data { get; set; }
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
}
```

## JSON Serialization

**Configuration:** [Program.cs:38-43](../Program.cs#L38-L43)

```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });
```

**Result:** Properties serialized as camelCase in JSON responses

## Model Validation

Uses DataAnnotations:
```csharp
[Required]
[EmailAddress]
public string Email { get; set; }

[Range(1, 100)]
public int Limit { get; set; }

[StringLength(255)]
public string Name { get; set; }
```

## Mapping Strategies

### Database → API
Repositories return database models, controllers/services map to API models:
```csharp
var dbModel = await _repository.GetAsync(id);
var apiModel = new CampaignResponse
{
    Id = dbModel.Id,
    Name = dbModel.Name,
    // ... map fields
};
```

### No Auto-Mapper
Manual mapping throughout codebase for explicitness and performance

## Model Conventions

1. **Suffix naming:**
   - `*DbModel` - Database entities
   - `*Request` - API request models
   - `*Response` - API response models
   - `*Dto` - Data transfer objects

2. **Properties:**
   - PascalCase in C#
   - camelCase in JSON (via serialization settings)

3. **Collections:**
   - Use `List<T>` for JSONB arrays
   - Use `Dictionary<string, T>` for JSONB objects

## Related Docs

- [04-database-schema.md](04-database-schema.md) - Database table structures
- [02-controllers-endpoints.md](02-controllers-endpoints.md) - Request/response models
- [03-authentication.md](03-authentication.md) - Auth models
