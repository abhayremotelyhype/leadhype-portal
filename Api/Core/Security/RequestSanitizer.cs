using System.Text.Json;
using System.Text.RegularExpressions;

namespace LeadHype.Api.Core.Security;

/// <summary>
/// Utility class for sanitizing request data before logging to prevent sensitive information exposure
/// </summary>
public static class RequestSanitizer
{
    private static readonly string[] SensitiveFields = {
        "password", "token", "key", "secret", "authorization", "auth", 
        "refreshtoken", "accesstoken", "connectionstring", "connectionstrings",
        "apikey", "refresh_token", "access_token", "session", "credential",
        "username", "email" // Be careful with PII
    };

    private static readonly Regex SensitiveFieldRegex = new(
        $@"\b({string.Join("|", SensitiveFields)})\b[""']?\s*[:=]\s*[""']?([^,\s""'}}]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    /// <summary>
    /// Sanitizes a request body string by replacing sensitive field values with [REDACTED]
    /// </summary>
    /// <param name="requestBody">The raw request body as string</param>
    /// <returns>Sanitized request body with sensitive values redacted</returns>
    public static string SanitizeRequestBody(string? requestBody)
    {
        if (string.IsNullOrEmpty(requestBody))
            return string.Empty;

        try
        {
            // Try to parse as JSON first for better sanitization
            var jsonDoc = JsonDocument.Parse(requestBody);
            return SanitizeJsonElement(jsonDoc.RootElement).ToString();
        }
        catch
        {
            // If not valid JSON, fall back to regex-based sanitization
            return SensitiveFieldRegex.Replace(requestBody, "$1: [REDACTED]");
        }
    }

    /// <summary>
    /// Sanitizes headers by removing sensitive authorization headers
    /// </summary>
    /// <param name="headers">Dictionary of headers</param>
    /// <returns>Sanitized headers with sensitive values redacted</returns>
    public static Dictionary<string, string> SanitizeHeaders(IHeaderDictionary headers)
    {
        var sanitized = new Dictionary<string, string>();
        
        foreach (var header in headers)
        {
            var key = header.Key.ToLowerInvariant();
            if (key.Contains("authorization") || key.Contains("token") || key.Contains("key"))
            {
                sanitized[header.Key] = "[REDACTED]";
            }
            else
            {
                sanitized[header.Key] = header.Value.ToString();
            }
        }
        
        return sanitized;
    }

    /// <summary>
    /// Sanitizes query parameters by redacting sensitive values
    /// </summary>
    /// <param name="queryParams">Query parameters</param>
    /// <returns>Sanitized query parameters</returns>
    public static Dictionary<string, string> SanitizeQueryParams(IQueryCollection queryParams)
    {
        var sanitized = new Dictionary<string, string>();
        
        foreach (var param in queryParams)
        {
            var key = param.Key.ToLowerInvariant();
            if (SensitiveFields.Any(field => key.Contains(field)))
            {
                sanitized[param.Key] = "[REDACTED]";
            }
            else
            {
                sanitized[param.Key] = param.Value.ToString();
            }
        }
        
        return sanitized;
    }

    private static JsonElement SanitizeJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var sanitizedObject = new Dictionary<string, object>();
                foreach (var property in element.EnumerateObject())
                {
                    var key = property.Name.ToLowerInvariant();
                    if (SensitiveFields.Any(field => key.Contains(field)))
                    {
                        sanitizedObject[property.Name] = "[REDACTED]";
                    }
                    else
                    {
                        sanitizedObject[property.Name] = SanitizeJsonElement(property.Value);
                    }
                }
                return JsonSerializer.SerializeToElement(sanitizedObject);

            case JsonValueKind.Array:
                var sanitizedArray = new List<object>();
                foreach (var item in element.EnumerateArray())
                {
                    sanitizedArray.Add(SanitizeJsonElement(item));
                }
                return JsonSerializer.SerializeToElement(sanitizedArray);

            default:
                return element;
        }
    }
}