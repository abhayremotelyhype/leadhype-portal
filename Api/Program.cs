// Program.cs (for .NET 6+)

using LeadHype.Api;
using LeadHype.Api.Core.Models;
using LeadHype.Api.Core.Database;
using LeadHype.Api.Core.Database.Repositories;
using LeadHype.Api.Managers;
using LeadHype.Api.ServiceApis;
using LeadHype.Api.Services;
using LeadHype.Api.Core.Models.Auth;
using LeadHype.Api.Middleware;
using LeadHype.Api.Controllers;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Scalar.AspNetCore;
using LeadHype.Api.Startup;
using LeadHype.Api.Authentication;
using LeadHype.Api.Core.Services.ExternalApi;
using LeadHype.Api.Core.Services.Analytics;

string? assemblyPath = System.Reflection.Assembly.GetEntryAssembly()?.Location ?? "";
string? directory = Path.GetDirectoryName(assemblyPath) ?? "";

DI.Build(directory);

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for larger headers
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestHeaderCount = 100;
    options.Limits.MaxRequestHeadersTotalSize = 32768; // 32KB for headers
});

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configure JSON serialization to use camelCase for property names
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure OpenAPI with Scalar
builder.Services.ConfigureOpenApi();

// PostgreSQL Database Services - Singleton for connection pooling
// Use production connection service with proper pooling to avoid connection exhaustion
builder.Services.AddSingleton<IDbConnectionService, PostgreSqlConnectionService>();
builder.Services.AddScoped<IDatabaseMigrationService, DatabaseMigrationService>();

// Repository services
builder.Services.AddScoped<ICampaignRepository, CampaignRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IEmailAccountRepository, EmailAccountRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISettingsRepository, SettingsRepository>();
builder.Services.AddScoped<IUserSessionRepository, UserSessionRepository>();
// Daily stats repositories - using new relational approach
builder.Services.AddScoped<IEmailAccountDailyStatEntryRepository, EmailAccountDailyStatEntryRepository>();
builder.Services.AddScoped<ICampaignDailyStatEntryRepository, CampaignDailyStatEntryRepository>();
// New event-based campaign statistics
builder.Services.AddScoped<ICampaignEventRepository, CampaignEventRepository>();
builder.Services.AddScoped<IEmailAccountStatsDateRepository, EmailAccountStatsDateRepository>();
// Campaign content repositories
builder.Services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
builder.Services.AddScoped<ILeadConversationRepository, LeadConversationRepository>();
builder.Services.AddScoped<ILeadEmailHistoryRepository, LeadEmailHistoryRepository>();
builder.Services.AddScoped<IClassifiedEmailRepository, ClassifiedEmailRepository>();

// API Key services
builder.Services.AddApiKeyServices();

builder.Services.AddHttpClient();

builder.Services.AddSingleton<MultiLogin>();
builder.Services.AddSingleton<TaskManager>();

// Services
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IClientStatsService, ClientStatsService>();
builder.Services.AddScoped<IUserStatsService, UserStatsService>();
// Daily stats services - using new relational approach
builder.Services.AddScoped<IEmailAccountDailyStatEntryService, EmailAccountDailyStatEntryService>();
// Note: CampaignDailyStatEntryService needs to be implemented if campaigns need daily stats

// Campaign business logic services
builder.Services.AddScoped<ICampaignService, CampaignService>();

// Webhook services
builder.Services.AddScoped<IWebhookRepository, WebhookRepository>();
builder.Services.AddScoped<IWebhookService, WebhookService>();

// Webhook event services
builder.Services.AddScoped<IWebhookEventConfigRepository, WebhookEventConfigRepository>();
builder.Services.AddScoped<IWebhookEventTriggerRepository, WebhookEventTriggerRepository>();
builder.Services.AddScoped<ICampaignMetricsMonitoringService, CampaignMetricsMonitoringService>();

// Authentication services
builder.Services.AddSingleton<JwtSettings>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDatabaseInitializationService, DatabaseInitializationService>();
builder.Services.AddSingleton<IRateLimitService, RateLimitService>();

builder.Services.AddScoped<SmartleadSyncService>();

// External API services
builder.Services.AddScoped<IRevReplyClassificationService, RevReplyClassificationService>();

// Background services
builder.Services.AddHostedService<SessionCleanupService>();
builder.Services.AddHostedService<LeadHype.Api.Services.BackgroundServices.WebhookEventMonitoringService>();

// JWT Authentication
var jwtSettings = new JwtSettings();
builder.Configuration.GetSection("JwtSettings").Bind(jwtSettings);

// Validate JWT settings
if (string.IsNullOrEmpty(jwtSettings.SecretKey) || 
    jwtSettings.SecretKey == "GENERATE_A_STRONG_32_CHARACTER_SECRET_KEY_HERE_FOR_PRODUCTION")
{
    throw new InvalidOperationException(
        "JWT SecretKey must be configured in appsettings.json. " +
        "Generate a strong 32+ character key for production use.");
}

if (jwtSettings.SecretKey.Length < 32)
{
    throw new InvalidOperationException(
        "JWT SecretKey must be at least 32 characters long for security.");
}

builder.Services.AddSingleton(jwtSettings);

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
        ClockSkew = TimeSpan.FromMinutes(5) // Allow 5 minutes clock skew tolerance
    };
})
.AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", options => { });
// Connection string validation
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException(
        "PostgreSQL connection string 'DefaultConnection' must be configured in appsettings.json");
}

// Configure CORS with security best practices
builder.Services.AddCors(options =>
{
    options.AddPolicy("SmartleadPolicy", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",  // Development frontend
                "http://localhost:3001",  // Development frontend (alternate port)
                "http://localhost:5010",  // Local backend serving frontend
                "https://leadhype-portal.com"  // Production domain - UPDATE THIS
            )
            .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
            .WithHeaders("Content-Type", "Authorization", "X-Refresh-Token")
            .AllowCredentials(); // Allows cookies/auth headers
    });
});

var app = builder.Build();

// Initialize PostgreSQL database with migrations
using (var scope = app.Services.CreateScope())
{
    var migrationService = scope.ServiceProvider.GetRequiredService<IDatabaseMigrationService>();
    await migrationService.MigrateAsync();
}

// Configure the HTTP request pipeline
// Add middleware to fix /api/docs redirect for Next.js proxy
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/api/docs" && !context.Request.Path.Value!.EndsWith("/"))
    {
        // Redirect to /api/docs/ with absolute path to avoid relative redirect issues
        context.Response.Redirect("/api/docs/", permanent: false);
        return;
    }
    await next();
});

// Configure Swagger UI
app.UseScalarUI();

// Add a redirect from /api/docs/api/openapi/v1.json to /api/openapi/v1.json
// to fix Scalar's relative URL issue
app.MapGet("/api/docs/api/openapi/v1.json", async (HttpContext context) =>
{
    context.Response.Redirect("/api/openapi/v1.json", permanent: false);
});

// Configure static files with cache headers
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Cache CSS/JS files for 1 hour but allow revalidation
        if (ctx.File.Name.EndsWith(".css") || ctx.File.Name.EndsWith(".js"))
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=3600, must-revalidate");
        }
        // Cache other static files for 24 hours
        else
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=86400");
        }
    }
});

// Add request logging middleware (only in development for debugging)
if (app.Environment.IsDevelopment())
{
    app.UseMiddleware<LeadHype.Api.Core.Middleware.RequestLoggingMiddleware>();
}

// Security headers middleware
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

// app.UseHttpsRedirection(); // Enable in production
app.UseCors("SmartleadPolicy");
app.UseAuthentication();
app.UseApiKeyAuthentication(); // Add API key authentication middleware
// Temporarily disable session validation middleware to debug authentication issues
// app.UseMiddleware<SessionValidationMiddleware>();
app.UseAuthorization();
app.MapControllers();
// app.MapFallbackToFile("index.html");

_ = Task.Run(async () =>
{
    // Initialize database with default admin user
    using var scope = app.Services.CreateScope();
    var initService = scope.ServiceProvider.GetRequiredService<IDatabaseInitializationService>();
    await initService.InitializeAsync();
    
    // Initialize SmartleadSyncService
    var mapperService = scope.ServiceProvider.GetRequiredService<SmartleadSyncService>();
    await mapperService.Fetch();
});

app.Run();