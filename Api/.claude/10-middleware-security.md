# Middleware & Security

**Pipeline:** [Program.cs:194-250](../Program.cs#L194-L250)

## Middleware Order

```
1. Static Files (wwwroot/)
2. Request Logging (dev only)
3. Security Headers
4. CORS
5. Authentication (JWT)
6. API Key Authentication
7. Session Validation (disabled)
8. Authorization
9. Controllers
10. Fallback (commented out)
```

## Middleware Details

### 1. Static Files
**[Program.cs:207-222](../Program.cs#L207-L222)**

```csharp
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // CSS/JS: 1 hour cache
        if (ctx.File.Name.EndsWith(".css") || ctx.File.Name.EndsWith(".js"))
            ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=3600, must-revalidate");
        // Others: 24 hour cache
        else
            ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=86400");
    }
});
```

### 2. Request Logging (Dev Only)
**[RequestLoggingMiddleware.cs](../Core/Middleware/RequestLoggingMiddleware.cs)**

Logs all HTTP requests in development environment only

### 3. Security Headers
**[Program.cs:231-241](../Program.cs#L231-L241)**

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:;");

    await next();
});
```

**Headers Added:**
- **X-Content-Type-Options:** Prevent MIME sniffing
- **X-Frame-Options:** Prevent clickjacking
- **X-XSS-Protection:** Browser XSS filter
- **Referrer-Policy:** Control referrer information
- **CSP:** Content Security Policy

### 4. CORS
**[Program.cs:167-182](../Program.cs#L167-L182)**

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("SmartleadPolicy", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "http://localhost:3001",
                "http://localhost:5010",
                "https://leadhype-portal.com"
            )
            .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
            .WithHeaders("Content-Type", "Authorization", "X-Refresh-Token")
            .AllowCredentials();
    });
});

app.UseCors("SmartleadPolicy");
```

**Allowed Origins:**
- localhost:3000 (Next.js dev)
- localhost:3001 (alternate)
- localhost:5010 (backend serves frontend)
- Production domain

### 5. Authentication (JWT)
**[Program.cs:138-156](../Program.cs#L138-L156)**

```csharp
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ClockSkew = TimeSpan.FromMinutes(5)
    };
})
.AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", options => { });
```

### 6. API Key Authentication
**[ApiKeyAuthenticationMiddleware.cs](../Core/Middleware/ApiKeyAuthenticationMiddleware.cs)**
**Extension:** [Program.cs:246](../Program.cs#L246)

```csharp
app.UseApiKeyAuthentication();
```

**Process:**
1. Extract key from X-API-Key header, Authorization: ApiKey, or query param
2. Skip if not /api/v1 route or already authenticated
3. Validate key hash against database
4. Check IP whitelist
5. Check rate limit
6. Create claims principal with permissions
7. Log usage after request

### 7. Session Validation (Disabled)
**[SessionValidationMiddleware.cs](../Core/Middleware/SessionValidationMiddleware.cs)**
**Status:** Commented out in [Program.cs:248](../Program.cs#L248)

```csharp
// app.UseMiddleware<SessionValidationMiddleware>();
```

Would validate active sessions from user_sessions table

### 8. Authorization
```csharp
app.UseAuthorization();
```

Checks `[Authorize]` attributes and role/permission claims

## Security Features

### Password Security
- **Hashing:** BCrypt with default work factor (10 rounds)
- **Storage:** Never store plaintext
- **Validation:** Constant-time comparison

### API Key Security
- **Generation:** Cryptographically secure random (32 bytes)
- **Storage:** SHA256 hash only
- **Prefix:** First 8 chars for display
- **Expiration:** Optional expiry timestamp
- **IP Whitelist:** Optional IP restrictions

### JWT Security
- **Secret Key:** Min 32 characters (validated on startup)
- **Algorithm:** HS256 (HMAC-SHA256)
- **Expiration:** 60 minutes (configurable)
- **Refresh Tokens:** 7 days (configurable)
- **Clock Skew:** 5 minutes tolerance

### Rate Limiting
**Login:** 5 attempts per IP+email per 15 minutes
**API Keys:** Configurable per key (default 10,000/hour)

### HTTPS
```csharp
// app.UseHttpsRedirection(); // Enable in production
```

Currently disabled, should enable for production

## Kestrel Configuration

**[Program.cs:31-35](../Program.cs#L31-L35)**

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestHeaderCount = 100;
    options.Limits.MaxRequestHeadersTotalSize = 32768; // 32KB
});
```

**[appsettings.json](../appsettings.json):**
```json
"Kestrel": {
  "Limits": {
    "MaxConcurrentConnections": 100,
    "MaxRequestBodySize": 10485760,
    "KeepAliveTimeout": "00:02:00",
    "RequestHeadersTimeout": "00:00:30"
  }
}
```

## Connection String Validation

**[Program.cs:159-164](../Program.cs#L159-L164)**

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException(
        "PostgreSQL connection string 'DefaultConnection' must be configured");
}
```

## JWT Settings Validation

**[Program.cs:122-134](../Program.cs#L122-L134)**

```csharp
if (string.IsNullOrEmpty(jwtSettings.SecretKey) ||
    jwtSettings.SecretKey == "GENERATE_A_STRONG_32_CHARACTER_SECRET_KEY_HERE_FOR_PRODUCTION")
{
    throw new InvalidOperationException("JWT SecretKey must be configured");
}

if (jwtSettings.SecretKey.Length < 32)
{
    throw new InvalidOperationException("JWT SecretKey must be at least 32 characters");
}
```

## Security Best Practices

### ✅ Implemented
- BCrypt password hashing
- JWT with strong secret key
- API key hashing (SHA256)
- CORS configuration
- Security headers
- Rate limiting
- Connection pooling limits
- Request size limits
- Input validation

### ⚠️ Consider for Production
- Enable HTTPS redirection
- Enable session validation middleware
- Add request throttling per endpoint
- Implement 2FA
- Add audit logging
- Set up WAF rules
- Enable database connection encryption

## Related Docs

- [03-authentication.md](03-authentication.md) - Auth flows
- [01-architecture.md](01-architecture.md) - Middleware pipeline order
- [11-dependencies.md](11-dependencies.md) - Security packages
