using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LeadHype.Api.Core.Models;
using LeadHype.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LeadHype.Api.Middleware
{
    public class ApiKeyAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;

        public ApiKeyAuthenticationMiddleware(RequestDelegate next, ILogger<ApiKeyAuthenticationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip API key auth for non-API routes or if already authenticated
            if ((!context.Request.Path.StartsWithSegments("/api/v1") && !context.Request.Path.StartsWithSegments("/api/clients")) || context.User.Identity?.IsAuthenticated == true)
            {
                await _next(context);
                return;
            }

            // Try to extract API key from header or query string
            var apiKey = ExtractApiKey(context.Request);
            
            if (string.IsNullOrEmpty(apiKey))
            {
                // No API key provided, continue to next middleware (might use JWT auth)
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var apiKeyService = context.RequestServices.GetRequiredService<IApiKeyService>();
            var authService = context.RequestServices.GetRequiredService<IAuthService>();

            try
            {
                // Validate the API key
                var validatedKey = await apiKeyService.ValidateApiKeyAsync(apiKey);
                
                if (validatedKey == null)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("{\"error\": \"Invalid or expired API key\"}");
                    return;
                }

                // Check IP whitelist if configured
                if (validatedKey.IpWhitelist.Any())
                {
                    var clientIp = GetClientIpAddress(context);
                    if (!validatedKey.IpWhitelist.Contains(clientIp))
                    {
                        _logger.LogWarning($"API key {validatedKey.KeyPrefix} used from unauthorized IP: {clientIp}");
                        context.Response.StatusCode = 403;
                        await context.Response.WriteAsync("{\"error\": \"Access denied from this IP address\"}");
                        return;
                    }
                }

                // Check rate limit
                var rateLimitOk = await apiKeyService.CheckRateLimitAsync(validatedKey.Id);
                if (!rateLimitOk)
                {
                    context.Response.StatusCode = 429;
                    context.Response.Headers.Add("X-RateLimit-Limit", validatedKey.RateLimit.ToString());
                    context.Response.Headers.Add("X-RateLimit-Remaining", "0");
                    context.Response.Headers.Add("X-RateLimit-Reset", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString());
                    await context.Response.WriteAsync("{\"error\": \"Rate limit exceeded\"}");
                    return;
                }

                // Get the user associated with this API key
                var user = await authService.GetUserByIdAsync(validatedKey.UserId);
                if (user == null || !user.IsActive)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("{\"error\": \"User account is inactive\"}");
                    return;
                }

                // Create claims for the API key user
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("ApiKeyId", validatedKey.Id),
                    new Claim("ApiKeyName", validatedKey.Name),
                    new Claim("AuthMethod", "ApiKey")
                };

                // Add permission claims
                foreach (var permission in validatedKey.Permissions)
                {
                    claims = claims.Append(new Claim("Permission", permission)).ToArray();
                }

                // Set the user context
                var identity = new ClaimsIdentity(claims, "ApiKey");
                var principal = new ClaimsPrincipal(identity);
                context.User = principal;

                // Store API key info for logging
                context.Items["ApiKeyId"] = validatedKey.Id;
                context.Items["ApiKeyName"] = validatedKey.Name;

                // Continue to the next middleware
                await _next(context);

                // Log API usage after response
                stopwatch.Stop();
                await apiKeyService.LogApiUsageAsync(
                    validatedKey.Id,
                    context.Request.Path,
                    context.Request.Method,
                    context.Response.StatusCode,
                    (int)stopwatch.ElapsedMilliseconds,
                    GetClientIpAddress(context),
                    context.Request.Headers["User-Agent"].ToString()
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during API key authentication");
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("{\"error\": \"Authentication error\"}");
            }
        }

        private string? ExtractApiKey(HttpRequest request)
        {
            // Check header first (preferred method)
            if (request.Headers.TryGetValue("X-API-Key", out var headerKey))
            {
                return headerKey.ToString();
            }

            // Check Authorization header with ApiKey scheme
            if (request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var auth = authHeader.ToString();
                if (auth.StartsWith("ApiKey ", StringComparison.OrdinalIgnoreCase))
                {
                    return auth.Substring(7);
                }
            }

            // Fallback to query parameter (not recommended for production)
            if (request.Query.TryGetValue("api_key", out var queryKey))
            {
                return queryKey.ToString();
            }

            return null;
        }

        private string GetClientIpAddress(HttpContext context)
        {
            // Check for proxy headers
            if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
                var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (ips.Length > 0)
                {
                    return ips[0].Trim();
                }
            }

            if (context.Request.Headers.ContainsKey("X-Real-IP"))
            {
                return context.Request.Headers["X-Real-IP"].ToString();
            }

            // Fallback to remote IP
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }

    // Extension method to register the middleware
    public static class ApiKeyAuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ApiKeyAuthenticationMiddleware>();
        }
    }
}