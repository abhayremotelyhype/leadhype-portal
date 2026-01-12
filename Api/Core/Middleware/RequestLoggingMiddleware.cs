using LeadHype.Api.Core.Security;
using System.Text;

namespace LeadHype.Api.Core.Middleware;

/// <summary>
/// Middleware for logging HTTP requests with sensitive data sanitization
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip logging for static files, health checks, and documentation
        var path = context.Request.Path.Value?.ToLower();
        if (path != null && (
            path.Contains("favicon") ||
            path.StartsWith("/openapi") ||
            path.StartsWith("/swagger") ||
            path.StartsWith("/scalar") ||
            path.EndsWith(".css") ||
            path.EndsWith(".js") ||
            path.EndsWith(".ico")))
        {
            await _next(context);
            return;
        }

        // Enable request body reading
        context.Request.EnableBuffering();

        var requestBody = string.Empty;
        if (context.Request.ContentLength > 0)
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0; // Reset for next middleware
        }

        // Sanitize and log request details
        var sanitizedHeaders = RequestSanitizer.SanitizeHeaders(context.Request.Headers);
        var sanitizedQuery = RequestSanitizer.SanitizeQueryParams(context.Request.Query);
        var sanitizedBody = RequestSanitizer.SanitizeRequestBody(requestBody);

        _logger.LogInformation("HTTP Request: {Method} {Path} from {IP}", 
            context.Request.Method, 
            context.Request.Path, 
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown");

        // Only log body for non-GET requests and if it's not too large
        if (!string.IsNullOrEmpty(sanitizedBody) && 
            context.Request.Method != HttpMethods.Get &&
            sanitizedBody.Length < 1000) // Limit body logging size
        {
            _logger.LogDebug("Request Body: {Body}", sanitizedBody);
        }

        if (sanitizedQuery.Any())
        {
            _logger.LogDebug("Query Parameters: {@QueryParams}", sanitizedQuery);
        }

        // Log critical headers (without sensitive data)
        var importantHeaders = sanitizedHeaders
            .Where(h => !h.Key.ToLowerInvariant().Contains("authorization") && 
                       !h.Key.ToLowerInvariant().Contains("token"))
            .Take(5) // Limit number of headers logged
            .ToDictionary(h => h.Key, h => h.Value);

        if (importantHeaders.Any())
        {
            _logger.LogDebug("Request Headers: {@Headers}", importantHeaders);
        }

        await _next(context);
    }
}