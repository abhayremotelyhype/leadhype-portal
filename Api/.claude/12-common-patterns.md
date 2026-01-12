# Common Code Patterns

## Async/Await Pattern

**All I/O operations are async:**
```csharp
public async Task<Campaign?> GetCampaignAsync(string id)
{
    using var connection = await _connectionService.GetConnectionAsync();
    return await connection.QuerySingleOrDefaultAsync<Campaign>(sql, new { Id = id });
}
```

**Never use `.Result` or `.Wait()` - causes deadlocks**

## Repository Pattern

```csharp
// Controller
public class CampaignController : ControllerBase
{
    private readonly ICampaignRepository _campaignRepo;

    public CampaignController(ICampaignRepository campaignRepo)
    {
        _campaignRepo = campaignRepo;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var campaign = await _campaignRepo.GetByIdAsync(id);
        if (campaign == null) return NotFound();
        return Ok(campaign);
    }
}
```

## Connection Management

```csharp
// Automatic disposal with 'using'
using var connection = await _connectionService.GetConnectionAsync();
var result = await connection.QueryAsync<T>(sql, parameters);
// Connection returned to pool here
```

## Error Handling

### Controller Pattern
```csharp
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateRequest request)
{
    try
    {
        var result = await _service.CreateAsync(request);
        return Ok(result);
    }
    catch (ArgumentException ex)
    {
        return BadRequest(new { message = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to create resource");
        return StatusCode(500, new { message = "Internal server error" });
    }
}
```

### Service Pattern
```csharp
public async Task<Result> OperationAsync()
{
    if (invalidInput)
        throw new ArgumentException("Invalid input");

    try
    {
        // operation
        return new Result { Success = true };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Operation failed");
        return new Result { Success = false, Error = ex.Message };
    }
}
```

## Pagination

```csharp
public async Task<PaginatedResponse<T>> GetPagedAsync(int page, int pageSize)
{
    page = Math.Max(1, page);
    pageSize = Math.Min(Math.Max(1, pageSize), 100); // Max 100

    var offset = (page - 1) * pageSize;

    const string countSql = "SELECT COUNT(*) FROM table WHERE ...";
    const string dataSql = "SELECT * FROM table WHERE ... LIMIT @Limit OFFSET @Offset";

    using var connection = await _connectionService.GetConnectionAsync();

    var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
    var data = await connection.QueryAsync<T>(dataSql, new { Limit = pageSize, Offset = offset });

    return new PaginatedResponse<T>
    {
        Data = data.ToList(),
        TotalCount = totalCount,
        CurrentPage = page,
        PageSize = pageSize,
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        HasPrevious = page > 1,
        HasNext = page < totalPages
    };
}
```

## UPSERT Pattern

```csharp
// PostgreSQL ON CONFLICT
const string sql = @"
    INSERT INTO table (id, name, value)
    VALUES (@Id, @Name, @Value)
    ON CONFLICT (id)
    DO UPDATE SET
        name = EXCLUDED.name,
        value = EXCLUDED.value,
        updated_at = NOW()";

using var connection = await _connectionService.GetConnectionAsync();
await connection.ExecuteAsync(sql, entity);
```

## Event Sourcing (Campaign Events)

```csharp
// Add event (UPSERT - increments if exists)
await _campaignEventRepository.AddEventAsync(
    campaignId: campaign.Id,
    eventType: "sent",
    count: 50,
    eventDate: DateTime.UtcNow.Date
);

// Aggregate events for time range
var stats = await _campaignEventRepository.GetAggregatedTotalsForCampaignsAsync(
    campaignIds: new[] { campaign.Id },
    startDate: startDate,
    endDate: endDate
);
```

## JWT Claims Access

```csharp
// In controller
private string? GetCurrentUserId()
{
    return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}

private string? GetUserRole()
{
    return User.FindFirst(ClaimTypes.Role)?.Value;
}

private bool IsAdmin()
{
    return GetUserRole() == UserRoles.Admin;
}

// Check permission
private bool HasPermission(string permission)
{
    return User.HasClaim("Permission", permission) ||
           User.HasClaim("Permission", ApiKeyPermissions.AdminAll);
}
```

## Role-Based Filtering

```csharp
public async Task<List<Campaign>> GetCampaignsAsync(string userId)
{
    var user = await _userRepository.GetByIdAsync(userId);

    if (user.Role == UserRoles.Admin)
    {
        // Admin sees all
        return await _campaignRepository.GetAllAsync();
    }
    else
    {
        // Regular user sees only assigned clients
        return await _campaignRepository.GetByClientIdsAsync(user.AssignedClientIds);
    }
}
```

## JSONB Handling

```csharp
// Write JSONB
const string sql = @"
    INSERT INTO table (id, tags, metadata)
    VALUES (@Id, @Tags::jsonb, @Metadata::jsonb)";

await connection.ExecuteAsync(sql, new
{
    Id = id,
    Tags = JsonSerializer.Serialize(tags),
    Metadata = JsonSerializer.Serialize(metadata)
});

// Read JSONB
const string sql = "SELECT id, tags, metadata FROM table WHERE id = @Id";
var result = await connection.QuerySingleAsync(sql, new { Id = id });

var tags = JsonSerializer.Deserialize<List<string>>(result.tags);
var metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(result.metadata);
```

## Batch Operations

```csharp
public async Task<Dictionary<string, TotalStats>> GetBatchStatsAsync(
    List<string> entityIds,
    DateTime startDate,
    DateTime endDate)
{
    const string sql = @"
        SELECT entity_id, SUM(sent) AS total_sent, SUM(opened) AS total_opened
        FROM stats_table
        WHERE entity_id = ANY(@EntityIds)
          AND stat_date BETWEEN @StartDate AND @EndDate
        GROUP BY entity_id";

    using var connection = await _connectionService.GetConnectionAsync();
    var results = await connection.QueryAsync(sql, new
    {
        EntityIds = entityIds.ToArray(),
        StartDate = startDate,
        EndDate = endDate
    });

    return results.ToDictionary(r => r.entity_id, r => new TotalStats { ... });
}
```

## Configuration Access

```csharp
// In Program.cs
var jwtSettings = new JwtSettings();
builder.Configuration.GetSection("JwtSettings").Bind(jwtSettings);
builder.Services.AddSingleton(jwtSettings);

// In service via DI
public class AuthService
{
    private readonly JwtSettings _jwtSettings;

    public AuthService(JwtSettings jwtSettings)
    {
        _jwtSettings = jwtSettings;
    }
}
```

## Logging

```csharp
public class MyService
{
    private readonly ILogger<MyService> _logger;

    public MyService(ILogger<MyService> logger)
    {
        _logger = logger;
    }

    public async Task OperationAsync()
    {
        _logger.LogInformation("Starting operation");

        try
        {
            // work
            _logger.LogDebug("Debug details: {Details}", details);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Operation failed with error");
            throw;
        }
    }
}
```

## Background Tasks

```csharp
// Startup initialization (non-blocking)
_ = Task.Run(async () =>
{
    using var scope = app.Services.CreateScope();
    var service = scope.ServiceProvider.GetRequiredService<IMyService>();
    await service.InitializeAsync();
});

// Hosted service
public class MyBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await DoWorkAsync();
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
```

## Email Normalization

```csharp
// Always lowercase for email lookups
var user = await _userRepository.GetByEmailAsync(email.ToLower());

// Store lowercase
user.Email = request.Email.ToLower();
```

## Nullable Reference Types

```csharp
// Enabled in .csproj
<Nullable>enable</Nullable>

// Required (non-nullable)
public string Name { get; set; } = null!;

// Optional (nullable)
public string? Description { get; set; }

// Null-conditional
var name = user?.FirstName ?? "Unknown";
```

## API Response Format

```csharp
// Success
return Ok(new { success = true, data = result });

// Error
return BadRequest(new { success = false, message = "Error details" });

// With pagination
return Ok(new PaginatedResponse<T>
{
    Data = items,
    TotalCount = count,
    CurrentPage = page,
    PageSize = pageSize
});
```

## Validation

```csharp
// Model validation
public class CreateRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Range(1, 100)]
    public int Limit { get; set; }
}

// Manual validation
if (string.IsNullOrEmpty(request.Name))
    return BadRequest(new { message = "Name is required" });

if (!ModelState.IsValid)
    return BadRequest(ModelState);
```

## Extension Methods

```csharp
// String extensions
public static class StringExtensions
{
    public static bool IsNullOrEmpty(this string? value)
        => string.IsNullOrEmpty(value);

    public static string ToSnakeCase(this string value)
        => Regex.Replace(value, "([a-z])([A-Z])", "$1_$2").ToLower();
}

// Usage
if (email.IsNullOrEmpty()) { ... }
```

## Related Docs

- [01-architecture.md](01-architecture.md) - DI patterns
- [05-repositories.md](05-repositories.md) - Data access patterns
- [07-services.md](07-services.md) - Service layer patterns
- [10-middleware-security.md](10-middleware-security.md) - Security patterns
