# Authentication System Documentation

## Overview

This ASP.NET Core project implements a dual authentication system supporting both JWT-based user authentication and API Key authentication. The system provides secure, scalable authentication with session management, rate limiting, and granular permissions.

### Authentication Methods

1. **JWT Authentication** - For user login via web interface, mobile apps, or traditional client applications
2. **API Key Authentication** - For programmatic API access with granular permissions and rate limiting

---

## JWT Authentication

### Core Components

- **AuthService** (`/Core/Services/Authentication/AuthService.cs`) - Main authentication service
- **User Model** (`/Core/Models/Database/User/User.cs`) - User entity with credentials and profile
- **UserSession Model** (`/Core/Models/Database/User/UserSession.cs`) - Session tracking
- **JwtSettings** (`/Core/Models/Auth/JwtSettings.cs`) - JWT configuration

### JWT Flow

#### 1. User Login

```csharp
// POST /api/auth/login
LoginRequest {
    Email: "user@example.com",
    Password: "password123"
}

// AuthService.AuthenticateAsync()
1. Validate credentials (BCrypt password verification)
2. Check if user is active
3. Generate access token (JWT) and refresh token
4. Create new UserSession record
5. Update user's last login timestamp
6. Return LoginResponse with tokens
```

**Key Code Snippet:**
```csharp
// Password verification
if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
{
    return new AuthResult {
        Success = false,
        ErrorType = AuthErrorType.InvalidCredentials
    };
}

// Generate tokens
var accessToken = GenerateAccessToken(user);
var refreshToken = GenerateRefreshToken();

// Create session
var session = new UserSession {
    UserId = user.Id,
    RefreshToken = refreshToken,
    RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7),
    IpAddress = ipAddress,
    UserAgent = userAgent,
    DeviceName = ParseDeviceName(userAgent)
};
```

#### 2. Access Token Structure

Access tokens are JWT tokens with the following claims:

```csharp
new Claim(ClaimTypes.NameIdentifier, user.Id),
new Claim(ClaimTypes.Email, user.Email),
new Claim(ClaimTypes.Name, user.Username),
new Claim(ClaimTypes.Role, user.Role),
new Claim("FirstName", user.FirstName ?? ""),
new Claim("LastName", user.LastName ?? ""),
new Claim("IsActive", user.IsActive.ToString())
```

**Token Expiration:**
- Access Token: 60 minutes (configurable via `JwtSettings.ExpirationInMinutes`)
- Refresh Token: 7 days (configurable via `JwtSettings.RefreshTokenExpirationInDays`)

#### 3. Token Refresh

```csharp
// POST /api/auth/refresh
RefreshRequest {
    RefreshToken: "base64-encoded-refresh-token"
}

// AuthService.RefreshTokenAsync()
1. Lookup UserSession by refresh token
2. Validate session is active and not expired
3. Verify user is still active
4. Generate new access token and refresh token
5. Update session with new refresh token
6. Update session's LastAccessedAt timestamp
7. Return new LoginResponse
```

**Key Code Snippet:**
```csharp
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
```

#### 4. Logout

```csharp
// POST /api/auth/logout
// AuthService.RevokeTokenAsync(refreshToken)
// Deletes the UserSession record, invalidating the refresh token
```

---

## Session Management

### UserSession Table

Tracks all active user sessions with metadata:

```csharp
public class UserSession {
    public string Id { get; set; }
    public string UserId { get; set; }
    public string RefreshToken { get; set; }
    public DateTime RefreshTokenExpiryTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastAccessedAt { get; set; }
    public string? DeviceName { get; set; }      // "iPhone", "Windows PC", etc.
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsActive { get; set; }
}
```

### Session Features

1. **Multi-Device Support** - Users can be logged in on multiple devices simultaneously
2. **Device Detection** - Parses user agent to identify device type (iPhone, Android, Windows PC, Mac, etc.)
3. **Session Tracking** - Tracks IP address, user agent, and last accessed time
4. **Session Management** - Users can view and revoke individual sessions or all sessions

### Session Repository Methods

```csharp
// Get all active sessions for a user
GetActiveSessionsByUserIdAsync(userId)

// Get expired sessions for cleanup
GetExpiredSessionsAsync()
// Returns sessions where:
// - refresh_token_expiry_time <= NOW
// - is_active = false
// - created_at <= NOW - 30 days

// Delete expired sessions
DeleteExpiredSessionsAsync()

// Revoke all user sessions (force logout everywhere)
DeleteAllUserSessionsAsync(userId)
```

---

## API Key Authentication

### Core Components

- **ApiKeyService** (`/Core/Services/Authentication/ApiKeyService.cs`) - API key management
- **ApiKeyAuthenticationMiddleware** (`/Core/Middleware/ApiKeyAuthenticationMiddleware.cs`) - Middleware for API key validation
- **ApiKey Model** (`/Core/Models/Security/ApiKey.cs`) - API key entity

### API Key Flow

#### 1. API Key Generation

```csharp
// POST /api/auth/generate-api-key
// AuthService.GenerateApiKeyAsync(userId)

1. Check if user already has an API key (revoke old one if exists)
2. Assign permissions based on user role:
   - Admin: "admin:all"
   - User: "read:campaigns", "read:emails", "read:leads", "read:clients"
3. Create API key via ApiKeyService
4. Update user record with API key
5. Return plain-text API key (only shown once!)
```

**Key Code Snippet:**
```csharp
// Generate secure random API key
var randomBytes = new byte[32];
using (var rng = RandomNumberGenerator.Create())
{
    rng.GetBytes(randomBytes);
}
var key = Convert.ToBase64String(randomBytes)
    .Replace("+", "-")
    .Replace("/", "_")
    .Replace("=", "");

// Hash the key before storing
var keyHash = HashApiKey(apiKeyPlain);
var keyPrefix = apiKeyPlain.Substring(0, 8); // For display

var apiKey = new ApiKey {
    KeyHash = keyHash,
    KeyPrefix = keyPrefix,
    UserId = userId,
    Permissions = permissions,
    RateLimit = 10000  // per hour
};
```

#### 2. API Key Validation (Middleware)

The `ApiKeyAuthenticationMiddleware` runs on every request and validates API keys:

```csharp
// Extract API key from request (priority order):
1. X-API-Key header
2. Authorization: ApiKey <key> header
3. api_key query parameter (not recommended)

// Validate API key
1. Hash the provided key
2. Lookup by hash in database
3. Check if expired (ExpiresAt)
4. Check IP whitelist (if configured)
5. Check rate limit (hourly window)
6. Verify user is active
7. Create ClaimsPrincipal with user and permission claims
8. Log API usage (endpoint, method, status, response time)
```

**Key Code Snippet:**
```csharp
// Validate API key
var validatedKey = await apiKeyService.ValidateApiKeyAsync(apiKey);
if (validatedKey == null) {
    context.Response.StatusCode = 401;
    return;
}

// Check IP whitelist
if (validatedKey.IpWhitelist.Any()) {
    var clientIp = GetClientIpAddress(context);
    if (!validatedKey.IpWhitelist.Contains(clientIp)) {
        context.Response.StatusCode = 403;
        return;
    }
}

// Check rate limit
var rateLimitOk = await apiKeyService.CheckRateLimitAsync(validatedKey.Id);
if (!rateLimitOk) {
    context.Response.StatusCode = 429;
    context.Response.Headers.Add("X-RateLimit-Limit", validatedKey.RateLimit.ToString());
    return;
}

// Set user context with permissions
var claims = new[] {
    new Claim(ClaimTypes.NameIdentifier, user.Id),
    new Claim(ClaimTypes.Role, user.Role),
    new Claim("ApiKeyId", validatedKey.Id),
    new Claim("AuthMethod", "ApiKey")
};
foreach (var permission in validatedKey.Permissions) {
    claims = claims.Append(new Claim("Permission", permission)).ToArray();
}
context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "ApiKey"));
```

#### 3. API Key Permissions

Granular permissions control what API endpoints can be accessed:

```csharp
public static class ApiPermissions {
    // Campaign permissions
    public const string ReadCampaigns = "read:campaigns";
    public const string WriteCampaigns = "write:campaigns";
    public const string DeleteCampaigns = "delete:campaigns";

    // Email account permissions
    public const string ReadEmails = "read:emails";
    public const string WriteEmails = "write:emails";
    public const string DeleteEmails = "delete:emails";

    // Client permissions
    public const string ReadClients = "read:clients";
    public const string WriteClients = "write:clients";
    public const string DeleteClients = "delete:clients";

    // Lead permissions
    public const string ReadLeads = "read:leads";
    public const string WriteLeads = "write:leads";
    public const string DeleteLeads = "delete:leads";

    // Analytics permissions
    public const string ReadAnalytics = "read:analytics";

    // Webhook permissions
    public const string ReadWebhooks = "read:webhooks";
    public const string WriteWebhooks = "write:webhooks";
    public const string DeleteWebhooks = "delete:webhooks";

    // Admin permission (all access)
    public const string AdminAll = "admin:all";
}
```

#### 4. API Key Security Features

**Rate Limiting:**
- Default: 10,000 requests per hour (configurable per key)
- Tracked hourly with automatic window reset
- Returns 429 status with rate limit headers when exceeded

**IP Whitelist:**
- Optional list of allowed IP addresses
- Checks X-Forwarded-For and X-Real-IP headers for proxy support
- Returns 403 status when IP not whitelisted

**Expiration:**
- Optional expiration date (ExpiresAt)
- Automatically rejects expired keys

**Usage Logging:**
- Logs every API request (endpoint, method, status, response time)
- Tracks IP address and user agent
- Records errors for debugging

---

## Rate Limiting

### RateLimitService

In-memory rate limiting for login attempts to prevent brute force attacks:

```csharp
public class RateLimitService {
    // Default: 5 attempts per 15 minutes
    Task<bool> IsAllowedAsync(string identifier, int maxAttempts = 5, TimeSpan? timeWindow = null)
    Task ResetAsync(string identifier)
}
```

**Features:**
- Uses `ConcurrentDictionary` for thread-safe in-memory storage
- Tracks attempts per identifier (typically email or IP)
- Automatic window reset after time period
- Automatic cleanup of expired entries every 5 minutes

**Usage Pattern:**
```csharp
// Before authentication
var identifier = $"login:{request.Email}:{ipAddress}";
if (!await _rateLimitService.IsAllowedAsync(identifier)) {
    return Unauthorized("Too many login attempts. Please try again later.");
}

// On successful login
await _rateLimitService.ResetAsync(identifier);
```

---

## User Management

### User Model

```csharp
public class User {
    public string Id { get; set; }
    public string Email { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }        // BCrypt hash
    public string Role { get; set; }                 // "Admin" or "User"
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public List<string> AssignedClientIds { get; set; }  // Client access control
    public string? ApiKey { get; set; }
    public DateTime? ApiKeyCreatedAt { get; set; }
}
```

### User Roles

```csharp
public static class UserRoles {
    public const string Admin = "Admin";
    public const string User = "User";
}
```

**Admin Permissions:**
- Full CRUD on all users
- Access to all clients
- Can assign/unassign clients to users
- API key gets "admin:all" permission

**User Permissions:**
- Can only access assigned clients (via AssignedClientIds)
- Cannot modify other users
- API key gets basic read permissions

### User CRUD Operations

```csharp
// Create user
CreateUserAsync(CreateUserRequest request)
- Validates email uniqueness
- Hashes password with BCrypt
- Sets default username to email if not provided
- Assigns role and client IDs

// Update user
UpdateUserAsync(userId, UpdateUserRequest request)
- Updates email, username, name, role, IsActive, AssignedClientIds
- Partial update (only provided fields are updated)

// Delete user
DeleteUserAsync(userId)
- Deletes all user sessions first
- Then deletes user record

// Change password
ChangePasswordAsync(userId, ChangePasswordRequest request)
- Verifies current password
- Hashes new password with BCrypt
- Updates user record

// Reset password (admin only)
ResetUserPasswordAsync(userId, newPassword)
- Directly sets new password (no current password verification)
- Used by admins to reset user passwords
```

---

## Security Features

### 1. Password Hashing

Uses **BCrypt** for secure password hashing:

```csharp
// Hash password on user creation
user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

// Verify password on login
if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)) {
    return Unauthorized();
}
```

**Benefits:**
- Adaptive hashing (computationally expensive to brute force)
- Automatic salt generation
- Industry-standard security

### 2. Secure Token Generation

**Refresh Tokens:**
```csharp
using var rng = RandomNumberGenerator.Create();
var randomBytes = new byte[64];
rng.GetBytes(randomBytes);
return Convert.ToBase64String(randomBytes);
```

**API Keys:**
```csharp
var randomBytes = new byte[32];
using (var rng = RandomNumberGenerator.Create()) {
    rng.GetBytes(randomBytes);
}
// URL-safe base64 encoding
return Convert.ToBase64String(randomBytes)
    .Replace("+", "-")
    .Replace("/", "_")
    .Replace("=", "");
```

### 3. API Key Hashing

API keys are hashed (SHA256) before storage:

```csharp
public string HashApiKey(string apiKey) {
    using (var sha256 = SHA256.Create()) {
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToBase64String(hashBytes);
    }
}
```

Only the key prefix (first 8 characters) is stored in plain text for display purposes.

### 4. Account Protection

**Inactive Account Check:**
```csharp
if (!user.IsActive) {
    return new AuthResult {
        Success = false,
        ErrorType = AuthErrorType.AccountInactive,
        ErrorMessage = "Your account has been deactivated. Please contact an administrator."
    };
}
```

**Session Expiration:**
- Access tokens expire after 60 minutes
- Refresh tokens expire after 7 days
- Sessions older than 30 days are automatically cleaned up
- Expired sessions are deleted from database

### 5. Device Tracking

Parses user agent to identify device type:

```csharp
private string? ParseDeviceName(string? userAgent) {
    if (userAgent.Contains("iPhone")) return "iPhone";
    if (userAgent.Contains("iPad")) return "iPad";
    if (userAgent.Contains("Android")) return "Android Device";
    if (userAgent.Contains("Windows")) return "Windows PC";
    if (userAgent.Contains("Macintosh")) return "Mac";
    if (userAgent.Contains("Linux")) return "Linux PC";
    return "Unknown Device";
}
```

Helps users identify suspicious login sessions.

---

## Common Patterns & Best Practices

### 1. Dual Authentication Support

The middleware chain supports both JWT and API Key authentication:

```csharp
// API Key middleware runs first
app.UseApiKeyAuthentication();  // Sets context.User if valid API key
app.UseAuthentication();        // JWT authentication
app.UseAuthorization();
```

If API key is valid, user context is set and JWT auth is skipped. If no API key, JWT auth runs.

### 2. Email Normalization

All emails are stored and queried in lowercase:

```csharp
var user = await _userRepository.GetByEmailAsync(request.Email.ToLower());
user.Email = request.Email.ToLower();
```

Prevents duplicate accounts with different casing.

### 3. Error Types

Structured error responses with specific error types:

```csharp
public enum AuthErrorType {
    InvalidCredentials,
    AccountInactive,
    UserNotFound,
    InvalidToken,
    TokenExpired,
    GeneralError
}

public class AuthResult {
    public bool Success { get; set; }
    public AuthErrorType? ErrorType { get; set; }
    public string? ErrorMessage { get; set; }
    public LoginResponse? LoginResponse { get; set; }
}
```

### 4. Token Response Structure

Complete login response includes user info and token metadata:

```csharp
public class LoginResponse {
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime RefreshTokenExpiresAt { get; set; }
    public UserInfo User { get; set; }
}
```

### 5. API Key Best Practices

**Creation:**
- API key is only shown once at creation
- Stored as hash in database
- Key prefix shown in UI for identification

**Usage:**
- Prefer `X-API-Key` header over query parameter
- Never log full API keys (use prefix only)
- Implement rate limiting per key
- Use IP whitelist for sensitive operations

**Revocation:**
- Old keys automatically revoked when generating new key
- Can manually revoke via API
- Revoked keys immediately stop working

### 6. Session Management Best Practices

**Cleanup:**
- Implement background job to delete expired sessions
- Delete all sessions when user is deleted
- Allow users to revoke individual sessions

**Security:**
- Track IP and user agent for anomaly detection
- Limit concurrent sessions per user (optional)
- Force logout on password change (optional)

---

## Quick Reference

### Authentication Endpoints

```
POST   /api/auth/login              - User login (returns JWT + refresh token)
POST   /api/auth/refresh            - Refresh access token
POST   /api/auth/logout             - Logout (revoke refresh token)
POST   /api/auth/register           - Create new user
PUT    /api/auth/change-password    - Change own password
POST   /api/auth/generate-api-key   - Generate API key for user
GET    /api/auth/sessions           - Get user's active sessions
DELETE /api/auth/sessions/:id       - Revoke specific session
DELETE /api/auth/sessions           - Revoke all sessions
```

### API Key Usage

```bash
# X-API-Key header (recommended)
curl -H "X-API-Key: your-api-key-here" https://api.example.com/api/v1/campaigns

# Authorization header
curl -H "Authorization: ApiKey your-api-key-here" https://api.example.com/api/v1/campaigns

# Query parameter (not recommended)
curl https://api.example.com/api/v1/campaigns?api_key=your-api-key-here
```

### JWT Usage

```bash
# Bearer token in Authorization header
curl -H "Authorization: Bearer your-jwt-token" https://api.example.com/api/campaigns
```

---

## Database Schema

### users table
```sql
- id: string (PK)
- email: string (unique)
- username: string
- password_hash: string
- role: string ("Admin" or "User")
- first_name: string (nullable)
- last_name: string (nullable)
- is_active: boolean
- created_at: timestamp
- last_login_at: timestamp (nullable)
- refresh_token: string (nullable, deprecated - use user_sessions)
- refresh_token_expiry_time: timestamp (nullable, deprecated)
- assigned_client_ids: jsonb
- api_key: string (nullable)
- api_key_created_at: timestamp (nullable)
- updated_at: timestamp
```

### user_sessions table
```sql
- id: string (PK)
- user_id: string (FK to users)
- refresh_token: string (unique)
- refresh_token_expiry_time: timestamp
- created_at: timestamp
- last_accessed_at: timestamp
- device_name: string (nullable)
- ip_address: string (nullable)
- user_agent: string (nullable)
- is_active: boolean
```

### api_keys table
```sql
- id: string (PK)
- key_hash: string (unique)
- key_prefix: string
- user_id: string (FK to users)
- name: string
- description: string (nullable)
- permissions: jsonb
- rate_limit: integer
- is_active: boolean
- last_used_at: timestamp (nullable)
- expires_at: timestamp (nullable)
- ip_whitelist: jsonb
- created_at: timestamp
- updated_at: timestamp
```

### api_key_usage table
```sql
- id: string (PK)
- api_key_id: string (FK to api_keys)
- endpoint: string (nullable)
- method: string (nullable)
- status_code: integer (nullable)
- response_time_ms: integer (nullable)
- ip_address: string (nullable)
- user_agent: string (nullable)
- request_body_size: integer (nullable)
- response_body_size: integer (nullable)
- error_message: string (nullable)
- created_at: timestamp
```

---

## Future Enhancements

Consider implementing:

1. **OAuth2/OIDC** - Social login (Google, Microsoft, GitHub)
2. **Two-Factor Authentication** - TOTP or SMS-based 2FA
3. **Password Reset** - Email-based password reset flow
4. **Email Verification** - Verify email on registration
5. **Audit Logging** - Comprehensive audit trail for security events
6. **Session Policies** - Max concurrent sessions, forced logout, etc.
7. **API Key Scopes** - More granular permission system
8. **Webhook Signing** - HMAC signing for webhook security
9. **Redis Rate Limiting** - Distributed rate limiting for multi-instance deployments
10. **Account Lockout** - Temporary lockout after failed login attempts
