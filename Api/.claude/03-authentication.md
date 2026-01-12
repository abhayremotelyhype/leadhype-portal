# Authentication System

## Overview

Dual authentication system supporting two methods:

1. **JWT Authentication** - Web application clients
   - Access tokens (short-lived: 60 minutes)
   - Refresh tokens (long-lived: 7 days)
   - Session tracking with IP and device info

2. **API Key Authentication** - External integrations
   - Permission-based access control
   - Rate limiting (default: 10,000 req/hour)
   - IP whitelist support
   - Usage tracking

**Files:**
- [AuthService.cs](../Core/Services/Authentication/AuthService.cs) - JWT auth logic
- [ApiKeyService.cs](../Core/Services/Authentication/ApiKeyService.cs) - API key management
- [ApiKeyAuthenticationMiddleware.cs](../Core/Middleware/ApiKeyAuthenticationMiddleware.cs) - API key validation
- [RateLimitService.cs](../Core/Services/RateLimitService.cs) - Rate limiting

---

## JWT Authentication Flow

### 1. Login

**Endpoint:** `POST /api/auth/login`

**Process:**
1. Rate limit check (5 attempts per IP+email per 15 minutes)
2. Retrieve user by email (lowercase normalized)
3. Verify password with BCrypt
4. Check if account is active
5. Generate access token (JWT) + refresh token (random)
6. Create UserSession record
7. Update user's last login timestamp
8. Return tokens + user info

**Code Flow:**
```csharp
// AuthController.cs
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    var rateLimitKey = $"login:{clientIp}:{request.Email.ToLower()}";

    // Rate limit: 5 attempts per 15 minutes
    var isAllowed = await _rateLimitService.IsAllowedAsync(rateLimitKey, 5, TimeSpan.FromMinutes(15));
    if (!isAllowed)
        return StatusCode(429, new { message = "Too many login attempts" });

    var result = await _authService.AuthenticateAsync(request, ipAddress, userAgent);

    if (!result.Success)
        return Unauthorized(new { message = result.ErrorMessage });

    // Reset rate limit on success
    await _rateLimitService.ResetAsync(rateLimitKey);

    return Ok(result.LoginResponse);
}

// AuthService.cs
public async Task<AuthResult> AuthenticateAsync(LoginRequest request, string? ipAddress, string? userAgent)
{
    var user = await _userRepository.GetByEmailAsync(request.Email.ToLower());

    // Verify password with BCrypt
    if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        return new AuthResult { Success = false, ErrorMessage = "Invalid email or password" };

    if (!user.IsActive)
        return new AuthResult { Success = false, ErrorMessage = "Account deactivated" };

    // Generate tokens
    var accessToken = GenerateAccessToken(user);
    var refreshToken = GenerateRefreshToken();

    // Create session
    var session = new UserSession
    {
        UserId = user.Id,
        RefreshToken = refreshToken,
        RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7),
        IpAddress = ipAddress,
        UserAgent = userAgent,
        DeviceName = ParseDeviceName(userAgent)
    };

    await _userSessionRepository.CreateAsync(session);

    return new AuthResult { Success = true, LoginResponse = new LoginResponse { ... } };
}
```

### 2. Token Structure

**Access Token (JWT):**
```json
{
  "nameid": "user-uuid",
  "email": "user@example.com",
  "unique_name": "username",
  "role": "Admin|User",
  "nbf": 1234567890,
  "exp": 1234571490,
  "iat": 1234567890,
  "iss": "LeadHype.Api",
  "aud": "leadhype-users"
}
```

**Claims:**
- `NameIdentifier`: User ID (UUID)
- `Email`: User email
- `Name`: Username
- `Role`: Admin or User
- Expiration: 60 minutes (configurable in appsettings.json: 43200 minutes = 30 days)

**Refresh Token:**
- Random 64-byte cryptographically secure string
- Stored in UserSession table (hashed)
- Expiration: 7 days (configurable: 90 days in appsettings.json)

### 3. Token Refresh

**Endpoint:** `POST /api/auth/refresh`

```csharp
public async Task<LoginResponse?> RefreshTokenAsync(string refreshToken)
{
    // Find session by refresh token
    var session = await _userSessionRepository.GetByRefreshTokenAsync(refreshToken);
    if (session == null) return null;

    var user = await _userRepository.GetByIdAsync(session.UserId);
    if (user == null || !user.IsActive) return null;

    // Generate new tokens
    var newAccessToken = GenerateAccessToken(user);
    var newRefreshToken = GenerateRefreshToken();

    // Update session
    session.RefreshToken = newRefreshToken;
    session.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
    session.LastAccessedAt = DateTime.UtcNow;

    await _userSessionRepository.UpdateAsync(session);

    return new LoginResponse { ... };
}
```

### 4. Logout

**Endpoint:** `POST /api/auth/logout`

Deletes UserSession by refresh token:
```csharp
[HttpPost("logout")]
[Authorize]
public async Task<IActionResult> Logout()
{
    var refreshToken = Request.Headers["X-Refresh-Token"].ToString();

    if (string.IsNullOrEmpty(refreshToken))
        return BadRequest(new { message = "Refresh token required" });

    await _authService.RevokeTokenAsync(refreshToken);
    return Ok(new { message = "Logged out successfully" });
}
```

**Logout All Sessions:** `POST /api/auth/logout-all` - Deletes all sessions for user

---

## Session Management

### UserSession Model

```csharp
public class UserSession
{
    public string Id { get; set; }              // UUID
    public string UserId { get; set; }          // User UUID
    public string RefreshToken { get; set; }    // Hashed refresh token
    public DateTime RefreshTokenExpiryTime { get; set; }
    public string? IpAddress { get; set; }      // Client IP
    public string? UserAgent { get; set; }      // Browser/client info
    public string? DeviceName { get; set; }     // Parsed device name
    public DateTime CreatedAt { get; set; }
    public DateTime LastAccessedAt { get; set; }
}
```

### Device Detection

Parses User-Agent to identify device:
```csharp
private string? ParseDeviceName(string? userAgent)
{
    if (string.IsNullOrEmpty(userAgent)) return "Unknown Device";

    if (userAgent.Contains("iPhone")) return "iPhone";
    if (userAgent.Contains("iPad")) return "iPad";
    if (userAgent.Contains("Android")) return "Android Device";
    if (userAgent.Contains("Windows NT")) return "Windows PC";
    if (userAgent.Contains("Macintosh")) return "Mac";
    if (userAgent.Contains("Linux")) return "Linux PC";

    return "Unknown Device";
}
```

### Session Cleanup

**Background Service:** `SessionCleanupService`

Runs every 1 hour, removes sessions older than 30 days:
```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        await _sessionRepository.DeleteExpiredSessionsAsync();
        await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
    }
}

// UserSessionRepository
public async Task<int> DeleteExpiredSessionsAsync()
{
    const string sql = @"
        DELETE FROM user_sessions
        WHERE refresh_token_expiry_time < @Now
           OR last_accessed_at < @ThirtyDaysAgo";

    using var connection = await _connectionService.GetConnectionAsync();
    return await connection.ExecuteAsync(sql, new
    {
        Now = DateTime.UtcNow,
        ThirtyDaysAgo = DateTime.UtcNow.AddDays(-30)
    });
}
```

---

## API Key Authentication

### API Key Model

```csharp
public class ApiKey
{
    public string Id { get; set; }                      // UUID
    public string KeyHash { get; set; }                 // SHA256 hash of actual key
    public string KeyPrefix { get; set; }               // First 8 chars (for display)
    public string UserId { get; set; }                  // Owner
    public string Name { get; set; }                    // Descriptive name
    public string? Description { get; set; }
    public List<string> Permissions { get; set; }       // Permission strings
    public int RateLimit { get; set; }                  // Requests per hour
    public bool IsActive { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public List<string> IpWhitelist { get; set; }       // Allowed IPs
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### Permission System

**Available Permissions:**
```csharp
public static class ApiKeyPermissions
{
    // Campaign permissions
    public const string ReadCampaigns = "ReadCampaigns";
    public const string WriteCampaigns = "WriteCampaigns";
    public const string DeleteCampaigns = "DeleteCampaigns";

    // Email account permissions
    public const string ReadEmailAccounts = "ReadEmailAccounts";
    public const string WriteEmailAccounts = "WriteEmailAccounts";
    public const string DeleteEmailAccounts = "DeleteEmailAccounts";

    // Client permissions
    public const string ReadClients = "ReadClients";
    public const string WriteClients = "WriteClients";
    public const string DeleteClients = "DeleteClients";

    // Webhook permissions
    public const string ReadWebhooks = "ReadWebhooks";
    public const string WriteWebhooks = "WriteWebhooks";
    public const string DeleteWebhooks = "DeleteWebhooks";

    // User permissions
    public const string ReadUsers = "ReadUsers";
    public const string WriteUsers = "WriteUsers";
    public const string DeleteUsers = "DeleteUsers";

    // Admin permission (all access)
    public const string AdminAll = "AdminAll";
}
```

### API Key Generation

**Endpoint:** `POST /api/apikeys`

```csharp
public async Task<string> CreateApiKeyAsync(ApiKey apiKey, string userId)
{
    // Generate cryptographically secure key
    var keyBytes = new byte[32];
    using (var rng = RandomNumberGenerator.Create())
    {
        rng.GetBytes(keyBytes);
    }

    var apiKeyString = Convert.ToBase64String(keyBytes);
    var keyHash = ComputeKeyHash(apiKeyString);

    apiKey.Id = Guid.NewGuid().ToString();
    apiKey.KeyHash = keyHash;
    apiKey.KeyPrefix = apiKeyString.Substring(0, 8);
    apiKey.UserId = userId;
    apiKey.IsActive = true;
    apiKey.CreatedAt = DateTime.UtcNow;
    apiKey.UpdatedAt = DateTime.UtcNow;

    // Validate permissions (user cannot grant permissions they don't have)
    var user = await _authService.GetUserByIdAsync(userId);
    if (user.Role != UserRoles.Admin)
    {
        // Remove admin-only permissions
        apiKey.Permissions = apiKey.Permissions
            .Where(p => p != ApiKeyPermissions.AdminAll)
            .ToList();
    }

    await _apiKeyRepository.CreateAsync(apiKey);

    return apiKeyString; // Return actual key (only shown once)
}

private string ComputeKeyHash(string apiKey)
{
    using var sha256 = SHA256.Create();
    var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
    return Convert.ToBase64String(hashBytes);
}
```

### API Key Validation Middleware

**File:** [ApiKeyAuthenticationMiddleware.cs](../Core/Middleware/ApiKeyAuthenticationMiddleware.cs)

**Flow:**
1. Extract API key from request (header, Authorization header, or query param)
2. Skip if not V1 API route or already authenticated
3. Hash provided key and lookup in database
4. Validate expiration
5. Check IP whitelist (if configured)
6. Check rate limit
7. Verify user is active
8. Create claims principal with permissions
9. Log usage after response

```csharp
public async Task InvokeAsync(HttpContext context)
{
    // Skip for non-V1 routes or if already authenticated
    if (!context.Request.Path.StartsWithSegments("/api/v1") ||
        context.User.Identity?.IsAuthenticated == true)
    {
        await _next(context);
        return;
    }

    var apiKey = ExtractApiKey(context.Request);
    if (string.IsNullOrEmpty(apiKey))
    {
        await _next(context);
        return;
    }

    // Validate API key
    var validatedKey = await apiKeyService.ValidateApiKeyAsync(apiKey);
    if (validatedKey == null)
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("{\"error\": \"Invalid or expired API key\"}");
        return;
    }

    // Check IP whitelist
    if (validatedKey.IpWhitelist.Any())
    {
        var clientIp = GetClientIpAddress(context);
        if (!validatedKey.IpWhitelist.Contains(clientIp))
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("{\"error\": \"Access denied from this IP\"}");
            return;
        }
    }

    // Check rate limit
    var rateLimitOk = await apiKeyService.CheckRateLimitAsync(validatedKey.Id);
    if (!rateLimitOk)
    {
        context.Response.StatusCode = 429;
        await context.Response.WriteAsync("{\"error\": \"Rate limit exceeded\"}");
        return;
    }

    // Get user
    var user = await authService.GetUserByIdAsync(validatedKey.UserId);
    if (user == null || !user.IsActive)
    {
        context.Response.StatusCode = 401;
        return;
    }

    // Create claims
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role),
        new Claim("ApiKeyId", validatedKey.Id),
        new Claim("AuthMethod", "ApiKey")
    };

    // Add permission claims
    foreach (var permission in validatedKey.Permissions)
        claims = claims.Append(new Claim("Permission", permission)).ToArray();

    var identity = new ClaimsIdentity(claims, "ApiKey");
    context.User = new ClaimsPrincipal(identity);

    await _next(context);

    // Log usage
    await apiKeyService.LogApiUsageAsync(validatedKey.Id, ...);
}
```

### API Key Extraction

Three methods (in order of preference):
1. **Header:** `X-API-Key: <key>`
2. **Authorization Header:** `Authorization: ApiKey <key>`
3. **Query Param:** `?api_key=<key>` (not recommended)

---

## Rate Limiting

**Service:** `RateLimitService` (Singleton)

In-memory rate limiting using ConcurrentDictionary:

```csharp
public class RateLimitService : IRateLimitService
{
    private readonly ConcurrentDictionary<string, RateLimitEntry> _rateLimits = new();

    public async Task<bool> IsAllowedAsync(string key, int maxAttempts, TimeSpan window)
    {
        var now = DateTime.UtcNow;

        _rateLimits.AddOrUpdate(key,
            // Add new entry
            k => new RateLimitEntry { Count = 1, WindowStart = now },
            // Update existing
            (k, existing) =>
            {
                if (now - existing.WindowStart > window)
                {
                    // Reset window
                    return new RateLimitEntry { Count = 1, WindowStart = now };
                }

                existing.Count++;
                return existing;
            });

        var entry = _rateLimits[key];
        return entry.Count <= maxAttempts;
    }

    public Task ResetAsync(string key)
    {
        _rateLimits.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
```

**Login Rate Limit:**
- Key: `login:{ip}:{email}`
- Limit: 5 attempts per 15 minutes
- Reset on successful login

**API Key Rate Limit:**
- Key: API Key ID
- Limit: Configurable per key (default 10,000 req/hour)
- Tracked in database (api_key_usage table)

---

## User Management

### User Model

```csharp
public class User
{
    public string Id { get; set; }                      // UUID
    public string Email { get; set; }                   // Lowercase, unique
    public string Username { get; set; }                // Lowercase
    public string PasswordHash { get; set; }            // BCrypt hash
    public string Role { get; set; }                    // "Admin" or "User"
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsActive { get; set; }
    public List<string> AssignedClientIds { get; set; } // For non-admin users
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? ApiKey { get; set; }                 // Legacy (deprecated)
    public DateTime? ApiKeyCreatedAt { get; set; }
}

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string User = "User";
}
```

### CRUD Operations

**Create User:**
```csharp
public async Task<User?> CreateUserAsync(CreateUserRequest request)
{
    var existingUser = await _userRepository.GetByEmailAsync(request.Email.ToLower());
    if (existingUser != null) return null;

    var user = new User
    {
        Email = request.Email.ToLower(),
        Username = request.Username?.ToLower() ?? request.Email.ToLower(),
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        Role = request.Role,
        IsActive = true,
        AssignedClientIds = request.AssignedClientIds ?? new List<string>()
    };

    await _userRepository.CreateAsync(user);
    return user;
}
```

**Password Management:**
```csharp
// Change own password
public async Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
{
    var user = await _userRepository.GetByIdAsync(userId);
    if (user == null) return false;

    // Verify current password
    if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
        return false;

    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
    return await _userRepository.UpdateAsync(user);
}

// Admin reset password
public async Task<bool> ResetUserPasswordAsync(string userId, string newPassword)
{
    var user = await _userRepository.GetByIdAsync(userId);
    if (user == null) return false;

    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
    return await _userRepository.UpdateAsync(user);
}
```

---

## Security Features

### 1. Password Hashing

Uses BCrypt with default work factor (10 rounds):
```csharp
var hash = BCrypt.Net.BCrypt.HashPassword(password);
var isValid = BCrypt.Net.BCrypt.Verify(password, hash);
```

### 2. Secure Token Generation

Cryptographically secure random token:
```csharp
private string GenerateRefreshToken()
{
    var randomBytes = new byte[64];
    using var rng = RandomNumberGenerator.Create();
    rng.GetBytes(randomBytes);
    return Convert.ToBase64String(randomBytes);
}
```

### 3. API Key Hashing

Keys stored as SHA256 hashes:
```csharp
private string ComputeKeyHash(string apiKey)
{
    using var sha256 = SHA256.Create();
    var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
    return Convert.ToBase64String(hashBytes);
}
```

### 4. Protected User Logic

**AuthController.cs:**
```csharp
private bool IsProtectedUser(User user, string? currentUserId)
{
    // Users cannot delete/modify themselves (prevents lockout)
    if (currentUserId != null && user.Id == currentUserId)
        return true;

    // Protect all admin users
    return user.Role == UserRoles.Admin;
}
```

**Admin users cannot:**
- Delete themselves
- Delete other admins
- Modify other admin's roles
- Be assigned to specific clients (they see all)

### 5. Account Status Check

All authentication checks `IsActive` flag:
```csharp
if (!user.IsActive)
{
    return new AuthResult
    {
        Success = false,
        ErrorMessage = "Your account has been deactivated"
    };
}
```

### 6. Session Expiration

Multiple expiration policies:
- **Access Token:** 60 minutes (configurable)
- **Refresh Token:** 7 days (configurable)
- **Inactive Sessions:** Deleted after 30 days of no use

### 7. Device Tracking

Sessions track device info for security monitoring:
```csharp
var session = new UserSession
{
    IpAddress = ipAddress,
    UserAgent = userAgent,
    DeviceName = ParseDeviceName(userAgent)
};
```

---

## Common Patterns

### 1. Dual Authentication

Middleware chain in [Program.cs:244-246](../Program.cs#L244-L246):
```csharp
app.UseAuthentication();        // JWT validation
app.UseApiKeyAuthentication();  // API key validation (custom middleware)
app.UseAuthorization();
```

### 2. Email Normalization

All emails stored/queried in lowercase:
```csharp
var user = await _userRepository.GetByEmailAsync(request.Email.ToLower());
user.Email = request.Email.ToLower();
```

### 3. Error Types

Structured error responses:
```csharp
public enum AuthErrorType
{
    InvalidCredentials,
    AccountInactive,
    SessionExpired,
    InvalidRefreshToken
}

public class AuthResult
{
    public bool Success { get; set; }
    public AuthErrorType? ErrorType { get; set; }
    public string? ErrorMessage { get; set; }
    public LoginResponse? LoginResponse { get; set; }
}
```

### 4. Token Response Structure

Consistent response format:
```csharp
public class LoginResponse
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime RefreshTokenExpiresAt { get; set; }
    public UserInfo User { get; set; }
}
```

---

## Quick Reference

### JWT Endpoints

| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/api/auth/login` | Login with email/password |
| POST | `/api/auth/refresh` | Refresh access token |
| POST | `/api/auth/logout` | Revoke session |
| POST | `/api/auth/logout-all` | Revoke all sessions |
| GET | `/api/auth/me` | Get current user |
| GET | `/api/auth/sessions` | List active sessions |
| DELETE | `/api/auth/sessions/{id}` | Revoke specific session |

### API Key Endpoints

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/apikeys` | List user's API keys |
| POST | `/api/apikeys` | Create API key |
| PUT | `/api/apikeys/{id}` | Update API key |
| DELETE | `/api/apikeys/{id}` | Revoke API key |
| GET | `/api/apikeys/{id}/usage` | Usage statistics |
| GET | `/api/apikeys/permissions` | Available permissions |

### Using API Keys

**Header (Recommended):**
```bash
curl -H "X-API-Key: <your-key>" https://api.example.com/api/v1/campaigns
```

**Authorization Header:**
```bash
curl -H "Authorization: ApiKey <your-key>" https://api.example.com/api/v1/campaigns
```

**Query Parameter (Not Recommended):**
```bash
curl https://api.example.com/api/v1/campaigns?api_key=<your-key>
```

### Using JWT

```bash
curl -H "Authorization: Bearer <access-token>" https://api.example.com/api/campaigns
```

---

## Database Schema

### users table
```sql
CREATE TABLE users (
    id TEXT PRIMARY KEY,
    email TEXT UNIQUE NOT NULL,
    username TEXT NOT NULL,
    password_hash TEXT NOT NULL,
    role TEXT NOT NULL,
    first_name TEXT,
    last_name TEXT,
    is_active BOOLEAN DEFAULT true,
    assigned_client_ids JSONB DEFAULT '[]',
    last_login_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    api_key TEXT,  -- Legacy, deprecated
    api_key_created_at TIMESTAMP
);
```

### user_sessions table
```sql
CREATE TABLE user_sessions (
    id TEXT PRIMARY KEY,
    user_id TEXT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    refresh_token TEXT UNIQUE NOT NULL,
    refresh_token_expiry_time TIMESTAMP NOT NULL,
    ip_address TEXT,
    user_agent TEXT,
    device_name TEXT,
    created_at TIMESTAMP DEFAULT NOW(),
    last_accessed_at TIMESTAMP DEFAULT NOW()
);
```

### api_keys table
```sql
CREATE TABLE api_keys (
    id TEXT PRIMARY KEY,
    key_hash TEXT UNIQUE NOT NULL,
    key_prefix TEXT NOT NULL,
    user_id TEXT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name TEXT NOT NULL,
    description TEXT,
    permissions JSONB NOT NULL DEFAULT '[]',
    rate_limit INTEGER DEFAULT 10000,
    is_active BOOLEAN DEFAULT true,
    last_used_at TIMESTAMP,
    expires_at TIMESTAMP,
    ip_whitelist JSONB DEFAULT '[]',
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);
```

### api_key_usage table
```sql
CREATE TABLE api_key_usage (
    id TEXT PRIMARY KEY,
    api_key_id TEXT NOT NULL REFERENCES api_keys(id) ON DELETE CASCADE,
    endpoint TEXT NOT NULL,
    method TEXT NOT NULL,
    status_code INTEGER NOT NULL,
    response_time_ms INTEGER,
    ip_address TEXT,
    user_agent TEXT,
    created_at TIMESTAMP DEFAULT NOW()
);
```

---

## Future Enhancements

**Potential additions:**
- OAuth2/OIDC integration (Google, Microsoft)
- Two-factor authentication (TOTP)
- Password reset via email
- Email verification
- Audit logging for sensitive operations
- API key rotation
- Webhook signatures for secure callbacks
