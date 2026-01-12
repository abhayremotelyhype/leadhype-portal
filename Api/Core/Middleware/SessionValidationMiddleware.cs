using LeadHype.Api.Core.Database.Repositories;
using System.Security.Claims;

namespace LeadHype.Api.Middleware
{
    public class SessionValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public SessionValidationMiddleware(RequestDelegate next, IServiceScopeFactory serviceScopeFactory)
        {
            _next = next;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip validation for auth endpoints and documentation only
            var path = context.Request.Path.Value?.ToLower();
            if (path != null && (path.StartsWith("/api/auth/login") || 
                                path.StartsWith("/api/auth/refresh") ||
                                path.StartsWith("/status") ||
                                path.StartsWith("/openapi") ||
                                path.StartsWith("/swagger") ||
                                path.StartsWith("/scalar") ||
                                path.Contains("favicon")))
            {
                await _next(context);
                return;
            }

            // For authenticated endpoints, require both Authorization header and refresh token
            var hasAuthHeader = context.Request.Headers.ContainsKey("Authorization");
            var refreshToken = context.Request.Headers["X-Refresh-Token"].FirstOrDefault();
            
            // If no auth header at all, let it pass (will be handled by JWT middleware)
            if (!hasAuthHeader)
            {
                await _next(context);
                return;
            }

            // If auth header present but no refresh token, this is a security concern
            // Require refresh token for proper session tracking
            if (string.IsNullOrEmpty(refreshToken))
            {
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"message\":\"Session token required for authenticated requests\",\"code\":\"SESSION_TOKEN_MISSING\"}");
                return;
            }

            // Only validate session if refresh token is provided
            using var scope = _serviceScopeFactory.CreateScope();
            var sessionRepository = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();
            
            try
            {
                var session = await sessionRepository.GetByRefreshTokenAsync(refreshToken);
                
                // If session doesn't exist, is inactive, or has no user_id, return 401
                if (session == null || !session.IsActive || string.IsNullOrEmpty(session.UserId))
                {
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"message\":\"Session has been revoked\",\"code\":\"SESSION_EXPIRED\"}");
                    return;
                }

                // Check if session has expired
                if (session.RefreshTokenExpiryTime <= DateTime.UtcNow)
                {
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"message\":\"Session has expired\",\"code\":\"SESSION_EXPIRED\"}");
                    return;
                }

                // Update last accessed time for active sessions
                session.LastAccessedAt = DateTime.UtcNow;
                await sessionRepository.UpdateAsync(session);
            }
            catch (Exception ex)
            {
                // Log error but don't expose details - security risk to continue on error
                // Return 401 on session validation errors for security
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"message\":\"Session validation failed\",\"code\":\"SESSION_VALIDATION_ERROR\"}");
                return;
            }

            await _next(context);
        }
    }
}