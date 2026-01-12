using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using LeadHype.Api.Core.Models.Auth;

namespace LeadHype.Api.Core.Models
{
    public class ApiKey
    {
        public string Id { get; set; } = string.Empty;
        public string KeyHash { get; set; } = string.Empty;
        public string KeyPrefix { get; set; } = string.Empty; // First 7 chars for display (sk_live_xxx...)
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string> Permissions { get; set; } = new();
        public int RateLimit { get; set; } = 1000; // Requests per hour
        public bool IsActive { get; set; } = true;
        public DateTime? LastUsedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public List<string> IpWhitelist { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property
        [JsonIgnore]
        public User? User { get; set; }
    }

    public class ApiKeyUsage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ApiKeyId { get; set; } = string.Empty;
        public string? Endpoint { get; set; }
        public string? Method { get; set; }
        public int? StatusCode { get; set; }
        public int? ResponseTimeMs { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public int? RequestBodySize { get; set; }
        public int? ResponseBodySize { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class CreateApiKeyRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string> Permissions { get; set; } = new();
        public int? RateLimit { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public List<string>? IpWhitelist { get; set; }
    }

    public class ApiKeyResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty; // Only shown once at creation
        public string KeyPrefix { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string> Permissions { get; set; } = new();
        public int RateLimit { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ApiKeyListResponse
    {
        public string Id { get; set; } = string.Empty;
        public string KeyPrefix { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string> Permissions { get; set; } = new();
        public int RateLimit { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public int UsageCount { get; set; }
        public int UsageToday { get; set; }
    }

    public static class ApiPermissions
    {
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
        
        // Admin permission
        public const string AdminAll = "admin:all";
        
        public static readonly List<string> AllPermissions = new()
        {
            ReadCampaigns, WriteCampaigns, DeleteCampaigns,
            ReadEmails, WriteEmails, DeleteEmails,
            ReadClients, WriteClients, DeleteClients,
            ReadLeads, WriteLeads, DeleteLeads,
            ReadAnalytics, 
            ReadWebhooks, WriteWebhooks, DeleteWebhooks,
            AdminAll
        };
        
        public static readonly Dictionary<string, string> PermissionDescriptions = new()
        {
            { ReadCampaigns, "Read campaign data" },
            { WriteCampaigns, "Create and update campaigns" },
            { DeleteCampaigns, "Delete campaigns" },
            { ReadEmails, "Read email account data" },
            { WriteEmails, "Create and update email accounts" },
            { DeleteEmails, "Delete email accounts" },
            { ReadClients, "Read client data" },
            { WriteClients, "Create and update clients" },
            { DeleteClients, "Delete clients" },
            { ReadLeads, "Read lead data" },
            { WriteLeads, "Create and update leads" },
            { DeleteLeads, "Delete leads" },
            { ReadAnalytics, "Read analytics and reports" },
            { ReadWebhooks, "Read webhook data" },
            { WriteWebhooks, "Create and update webhooks" },
            { DeleteWebhooks, "Delete webhooks" },
            { AdminAll, "Full administrative access" }
        };
    }
}