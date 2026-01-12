using Microsoft.OpenApi.Models;
using System.Reflection;
using Scalar.AspNetCore;

namespace LeadHype.Api.Startup
{
    public static class ScalarConfiguration
    {
        public static void ConfigureOpenApi(this IServiceCollection services)
        {
            services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            });

            services.AddOpenApi("v1", options =>
            {
                options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
                
                // Configure document info
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    document.Info = new OpenApiInfo
                    {
                        Title = "LeadHype API",
                        Version = "v1",
                        Description = "Public API for LeadHype email campaign management - for registered users with API keys",
                        Contact = new OpenApiContact
                        {
                            Name = "LeadHype Support",
                            Email = "support@leadhype.com",
                            Url = new Uri("https://leadhype.com")
                        },
                        License = new OpenApiLicense
                        {
                            Name = "Commercial License",
                            Url = new Uri("https://leadhype.com/terms")
                        }
                    };
                    
                    // Don't set servers - let Scalar use the current browser URL
                    document.Servers = new List<OpenApiServer>();
                    
                    // Add API Key security scheme for V1 endpoints
                    document.Components ??= new OpenApiComponents();
                    document.Components.SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>
                    {
                        ["ApiKey"] = new OpenApiSecurityScheme
                        {
                            Name = "X-API-Key",
                            Type = SecuritySchemeType.ApiKey,
                            In = ParameterLocation.Header,
                            Description = "API Key authentication header. Example: \"X-API-Key: {your-api-key}\""
                        }
                    };
                    
                    return Task.CompletedTask;
                });
                
                // Document transformer to filter and add security
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    // Filter paths to only include V1 endpoints
                    var v1Paths = new Dictionary<string, OpenApiPathItem>();
                    
                    foreach (var path in document.Paths)
                    {
                        if (path.Key.Contains("/api/v1/"))
                        {
                            // Add security to all operations in this path
                            foreach (var operation in path.Value.Operations)
                            {
                                operation.Value.Security = new List<OpenApiSecurityRequirement>
                                {
                                    new OpenApiSecurityRequirement
                                    {
                                        {
                                            new OpenApiSecurityScheme
                                            {
                                                Reference = new OpenApiReference
                                                {
                                                    Type = ReferenceType.SecurityScheme,
                                                    Id = "ApiKey"
                                                }
                                            },
                                            Array.Empty<string>()
                                        }
                                    }
                                };
                            }
                            v1Paths[path.Key] = path.Value;
                        }
                    }
                    
                    document.Paths = new OpenApiPaths();
                    foreach (var path in v1Paths)
                    {
                        document.Paths.Add(path.Key, path.Value);
                    }
                    
                    return Task.CompletedTask;
                });
            });
        }

        public static void UseScalarUI(this WebApplication app)
        {
            // Map OpenAPI endpoint at /api/openapi/v1.json to work with Next.js proxy
            app.MapOpenApi("/api/openapi/{documentName}.json");

            // Use Scalar for API documentation at /api/docs/ (with trailing slash)
            app.MapScalarApiReference("/api/docs/", options =>
            {
                options.Title = "LeadHype API Documentation";
                options.Theme = ScalarTheme.BluePlanet;
                options.ShowSidebar = true;
                options.SearchHotKey = "k";
                options.Favicon = "/favicon.ico";
                options.OpenApiRoutePattern = "/api/openapi/v1.json";
                options.CustomCss = @"
                    :root {
                        --scalar-sidebar-width: 300px;
                    }
                    .sidebar {
                        --scalar-sidebar-background-1: #1a1a1a;
                        --scalar-sidebar-item-hover-color: #2563eb;
                    }
                    .api-client {
                        --scalar-background-1: #ffffff;
                    }";
            });
        }
    }
}