using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using LeadHype.Api.Core.Database.Repositories;
using LeadHype.Api.Core.Models.API.Requests;
using LeadHype.Api.Core.Models.API.Responses;
using LeadHype.Api.Core.Models.Auth;
using LeadHype.Api.Models;
using LeadHype.Api.Models.UI;
using LeadHype.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeadHype.Api.Controllers.V1;

/// <summary>
/// Client Management API v1
/// 
/// Complete RESTful API for managing clients in the LeadHype system.
/// All endpoints require administrator privileges and API key authentication.
/// 
/// Clients represent organizations or individuals that own campaigns and email accounts.
/// Each client can have multiple campaigns and email accounts associated with them.
/// </summary>
[ApiController]
[Route("api/v1/clients")]
[Authorize(AuthenticationSchemes = "ApiKey")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
[Tags("Clients V1")]
public class ClientsV1Controller : ControllerBase
{
    private readonly IClientRepository _clientRepository;
    private readonly ICampaignRepository _campaignRepository;
    private readonly IEmailAccountRepository _emailAccountRepository;
    private readonly IClientStatsService _clientStatsService;
    private readonly ILogger<ClientsV1Controller> _logger;
    private readonly IAuthService _authService;

    // Predefined color palette for better consistency and visual appeal
    private static readonly string[] ClientColors = {
        "#EF4444", // Red
        "#F97316", // Orange  
        "#F59E0B", // Amber
        "#EAB308", // Yellow
        "#84CC16", // Lime
        "#22C55E", // Green
        "#10B981", // Emerald
        "#14B8A6", // Teal
        "#06B6D4", // Cyan
        "#0EA5E9", // Sky
        "#3B82F6", // Blue
        "#6366F1", // Indigo
        "#8B5CF6", // Violet
        "#A855F7", // Purple
        "#D946EF", // Fuchsia
        "#EC4899", // Pink
        "#F43F5E"  // Rose
    };

    public ClientsV1Controller(
        IClientRepository clientRepository,
        ICampaignRepository campaignRepository,
        IEmailAccountRepository emailAccountRepository,
        IClientStatsService clientStatsService,
        ILogger<ClientsV1Controller> logger,
        IAuthService authService)
    {
        _clientRepository = clientRepository;
        _campaignRepository = campaignRepository;
        _emailAccountRepository = emailAccountRepository;
        _clientStatsService = clientStatsService;
        _logger = logger;
        _authService = authService;
    }

    private string GetRandomColor()
    {
        var random = new Random();
        return ClientColors[random.Next(ClientColors.Length)];
    }

    private bool IsAdmin()
    {
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        return userRole == UserRoles.Admin;
    }

    private async Task<List<string>?> GetUserAssignedClientIds()
    {
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        
        // Admin users can see all clients
        if (userRole == UserRoles.Admin)
        {
            return null; // null means no filtering
        }
        
        // Regular users can only see assigned clients
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return new List<string>(); // Empty list means no access
        
        var user = await _authService.GetUserByIdAsync(userId);
        return user?.AssignedClientIds ?? new List<string>();
    }

    /// <summary>
    /// Get client statistics using POST to avoid header size limitations when filtering by many client IDs
    /// </summary>
    /// <param name="request">Filter request with client IDs, pagination and date filters</param>
    /// <returns>Client statistics with reply timing and engagement metrics</returns>
    /// <response code="200">Client statistics retrieved successfully</response>
    [HttpPost("stats")]
    [ProducesResponseType(typeof(ClientStatsCollectionResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClientStatsFiltered([FromBody] ClientStatsFilterRequest request)
    {
        try
        {
            _logger.LogInformation("GetClientStatsFiltered called with FilterByUserId: '{FilterByUserId}'", request.FilterByUserId ?? "null");
            if (!string.IsNullOrEmpty(request.FilterByUserId))
            {
                _logger.LogInformation("User filter is being applied for user: {UserId}", request.FilterByUserId);
            }
            else
            {
                _logger.LogInformation("No user filter applied - showing all clients");
            }
            // Get user's assigned client IDs for filtering
            var assignedClientIds = await GetUserAssignedClientIds();
            
            // If user has no assigned clients, return empty result
            if (assignedClientIds != null && !assignedClientIds.Any())
            {
                return Ok(new ClientStatsCollectionResponse
                {
                    Clients = new List<ClientStatsResponse>(),
                    TotalCount = 0,
                    GeneratedAt = DateTime.UtcNow,
                    Pagination = new PaginationInfo
                    {
                        Page = request.Page,
                        PageSize = request.PageSize,
                        TotalPages = 0,
                        HasNextPage = false,
                        HasPreviousPage = false
                    }
                });
            }

            // Apply client ID filtering - intersection of assigned clients and requested clients
            var effectiveClientIds = request.ClientIds;
            if (assignedClientIds != null && effectiveClientIds != null && effectiveClientIds.Any())
            {
                effectiveClientIds = effectiveClientIds.Intersect(assignedClientIds).ToArray();
            }
            else if (assignedClientIds != null)
            {
                effectiveClientIds = assignedClientIds.ToArray();
            }

            // Use the existing service to get stats
            var statsResponse = await _clientStatsService.GetAllClientStatsAsync(
                request.Page, 
                request.PageSize, 
                request.SortBy, 
                request.SortDescending, 
                effectiveClientIds, 
                request.StartDate, 
                request.EndDate,
                request.ClientStatus,
                request.FilterByUserId
            );

            return Ok(statsResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting client statistics via POST");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get all clients - Retrieves paginated list with search. Features: pagination, full-text search across name/email/company, includes campaign/email counts, sorted by creation date.
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="limit">Items per page (default: 50, max: 100)</param>
    /// <param name="search">Search query for name, email, or company</param>
    /// <returns>Paginated list of clients with campaign and email account counts</returns>
    /// <response code="200">Returns the paginated list of clients with enriched metadata including campaign and email account counts</response>
    /// <example>
    /// Sample response:
    /// {
    ///   "success": true,
    ///   "data": [
    ///     {
    ///       "id": "client-001",
    ///       "name": "Acme Corporation",
    ///       "email": "contact@acme.com",
    ///       "company": "Acme Corp",
    ///       "status": "active",
    ///       "notes": "Premium client with multiple campaigns",
    ///       "createdAt": "2024-01-15T08:30:00.000Z",
    ///       "updatedAt": "2024-01-20T14:22:00.000Z",
    ///       "campaignCount": 12,
    ///       "activeCampaigns": 8,
    ///       "emailAccountCount": 25
    ///     },
    ///     {
    ///       "id": "client-002", 
    ///       "name": "TechStartup Inc",
    ///       "email": "hello@techstartup.io",
    ///       "company": "TechStartup Inc",
    ///       "status": "active",
    ///       "notes": null,
    ///       "createdAt": "2024-01-18T10:15:00.000Z",
    ///       "updatedAt": "2024-01-18T10:15:00.000Z",
    ///       "campaignCount": 3,
    ///       "activeCampaigns": 2,
    ///       "emailAccountCount": 5
    ///     }
    ///   ],
    ///   "pagination": {
    ///     "totalCount": 47,
    ///     "page": 1,
    ///     "limit": 50,
    ///     "totalPages": 1
    ///   }
    /// }
    /// </example>
    [HttpGet]
    [ProducesResponseType(typeof(ClientListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClients(
        [FromQuery] int page = 1, 
        [FromQuery] int limit = 50, 
        [FromQuery] string? search = null)
    {
        try
        {
            // Only admins can access client endpoints
            if (!IsAdmin())
            {
                return Forbid();
            }

            // Validate pagination parameters
            page = Math.Max(1, page);
            limit = Math.Clamp(limit, 1, 100);

            var clients = (await _clientRepository.GetAllAsync()).ToList();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                clients = clients.Where(c => 
                    c.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (c.Email?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                    (c.Company?.Contains(search, StringComparison.OrdinalIgnoreCase) == true))
                    .ToList();
            }

            // Sort by creation date ascending (oldest first)
            clients = clients.OrderBy(c => c.CreatedAt).ToList();

            // Enrich with additional stats
            var enrichedClients = new List<Client>();
            
            foreach (var client in clients)
            {
                var campaigns = (await _campaignRepository.GetByClientIdAsync(client.Id)).ToList();
                client.CampaignCount = campaigns.Count;
                client.ActiveCampaigns = campaigns.Count(c => c.Status?.ToLower() == "active");

                var emailAccounts = (await _emailAccountRepository.GetByClientIdAsync(client.Id)).ToList();
                client.EmailAccountCount = emailAccounts.Count;

                enrichedClients.Add(client);
            }

            // Apply pagination and create response DTOs
            var totalCount = enrichedClients.Count;
            var paginatedClients = enrichedClients
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(c => new ClientResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Email = c.Email,
                    Company = c.Company,
                    Status = c.Status,
                    Notes = c.Notes,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    CampaignCount = c.CampaignCount,
                    ActiveCampaigns = c.ActiveCampaigns,
                    EmailAccountCount = c.EmailAccountCount
                })
                .ToList();

            return Ok(new 
            { 
                success = true,
                data = paginatedClients, 
                pagination = new
                {
                    totalCount,
                    page,
                    limit,
                    totalPages = (int)Math.Ceiling(totalCount / (double)limit)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving clients");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get client by ID - Retrieves specific client details with enriched campaign and email account counts.
    /// </summary>
    /// <param name="id">Client ID</param>
    /// <returns>Client details with enriched metadata</returns>
    /// <response code="200">Client details retrieved successfully</response>
    /// <response code="404">Client not found</response>
    /// <example>
    /// Sample response:
    /// {
    ///   "success": true,
    ///   "data": {
    ///     "id": "client-001",
    ///     "name": "Acme Corporation",
    ///     "email": "contact@acme.com",
    ///     "company": "Acme Corp",
    ///     "status": "active",
    ///     "notes": "Premium enterprise client",
    ///     "createdAt": "2024-01-15T08:30:00.000Z",
    ///     "updatedAt": "2024-01-20T14:22:00.000Z",
    ///     "campaignCount": 12,
    ///     "activeCampaigns": 8,
    ///     "emailAccountCount": 25
    ///   }
    /// }
    /// </example>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ClientDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClient(string id)
    {
        try
        {
            // Only admins can access client endpoints
            if (!IsAdmin())
            {
                return Forbid();
            }

            var client = await _clientRepository.GetByIdAsync(id);
            if (client == null)
            {
                return NotFound(new { success = false, message = "Client not found" });
            }

            // Enrich with campaign and email account data
            var campaigns = (await _campaignRepository.GetByClientIdAsync(client.Id)).ToList();
            client.CampaignCount = campaigns.Count;
            client.ActiveCampaigns = campaigns.Count(c => c.Status?.ToLower() == "active");

            var emailAccounts = (await _emailAccountRepository.GetByClientIdAsync(client.Id)).ToList();
            client.EmailAccountCount = emailAccounts.Count;

            var clientDto = new ClientResponseDto
            {
                Id = client.Id,
                Name = client.Name,
                Email = client.Email,
                Company = client.Company,
                Status = client.Status,
                Notes = client.Notes,
                CreatedAt = client.CreatedAt,
                UpdatedAt = client.UpdatedAt,
                CampaignCount = client.CampaignCount,
                ActiveCampaigns = client.ActiveCampaigns,
                EmailAccountCount = client.EmailAccountCount
            };

            return Ok(new { success = true, data = clientDto });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving client {ClientId}", id);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create a new client - Creates client for organizing campaigns and email accounts. Required: name (max 200 chars). Optional: email, company, status, notes. Auto-generates: ID, timestamps, color.
    /// </summary>
    /// <param name="request">Client creation data</param>
    /// <returns>Created client with generated ID and metadata</returns>
    /// <response code="201">Client created successfully</response>
    /// <response code="400">Invalid request data or validation errors</response>
    /// <example>
    /// Sample response:
    /// {
    ///   "success": true,
    ///   "data": {
    ///     "id": "client-abc123def456",
    ///     "name": "TechCorp Solutions",
    ///     "email": "contact@techcorp.com", 
    ///     "company": "TechCorp Inc",
    ///     "status": "active",
    ///     "notes": "New enterprise client with high volume needs",
    ///     "createdAt": "2024-01-25T10:30:45.123Z",
    ///     "updatedAt": "2024-01-25T10:30:45.123Z",
    ///     "campaignCount": 0,
    ///     "activeCampaigns": 0,
    ///     "emailAccountCount": 0
    ///   },
    ///   "message": "Client created successfully"
    /// }
    /// </example>
    [HttpPost]
    [ProducesResponseType(typeof(ClientCreationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateClient([FromBody] CreateClientRequest request)
    {
        try
        {
            // Only admins can create clients
            if (!IsAdmin())
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid request data", errors = ModelState });
            }

            var client = new Client
            {
                Name = request.Name,
                Email = request.Email,
                Company = request.Company,
                Status = request.Status ?? "active",
                Color = request.Color ?? GetRandomColor(),
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _clientRepository.CreateAsync(client);

            _logger.LogInformation("Created new client: {ClientName} (ID: {ClientId})", client.Name, client.Id);

            var clientDto = new ClientResponseDto
            {
                Id = client.Id,
                Name = client.Name,
                Email = client.Email,
                Company = client.Company,
                Status = client.Status,
                Notes = client.Notes,
                CreatedAt = client.CreatedAt,
                UpdatedAt = client.UpdatedAt,
                CampaignCount = 0,
                ActiveCampaigns = 0,
                EmailAccountCount = 0
            };

            return CreatedAtAction(nameof(GetClient), new { id = client.Id }, 
                new { success = true, data = clientDto, message = "Client created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating client");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update an existing client - Updates client information including name, email, company, status, and notes.
    /// </summary>
    /// <param name="id">Client ID</param>
    /// <param name="request">Client update data</param>
    /// <returns>Updated client with enriched metadata</returns>
    /// <response code="200">Client updated successfully</response>
    /// <response code="400">Invalid request data or validation errors</response>
    /// <response code="404">Client not found</response>
    /// <example>
    /// Sample response:
    /// {
    ///   "success": true,
    ///   "data": {
    ///     "id": "client-001",
    ///     "name": "Acme Corporation Updated",
    ///     "email": "newemail@acme.com",
    ///     "company": "Acme Corp Ltd",
    ///     "status": "active",
    ///     "notes": "Updated notes for premium client",
    ///     "createdAt": "2024-01-15T08:30:00.000Z",
    ///     "updatedAt": "2024-01-25T16:45:30.000Z",
    ///     "campaignCount": 12,
    ///     "activeCampaigns": 8,
    ///     "emailAccountCount": 25
    ///   },
    ///   "message": "Client updated successfully"
    /// }
    /// </example>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ClientUpdateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateClient(string id, [FromBody] UpdateClientRequest request)
    {
        try
        {
            // Only admins can update clients
            if (!IsAdmin())
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid request data", errors = ModelState });
            }

            var existingClient = await _clientRepository.GetByIdAsync(id);
            if (existingClient == null)
            {
                return NotFound(new { success = false, message = "Client not found" });
            }

            // Update client properties
            existingClient.Name = request.Name;
            existingClient.Email = request.Email;
            existingClient.Company = request.Company;
            existingClient.Status = request.Status ?? existingClient.Status;
            existingClient.Color = request.Color ?? existingClient.Color;
            existingClient.Notes = request.Notes;
            existingClient.UpdatedAt = DateTime.UtcNow;

            await _clientRepository.UpdateAsync(existingClient);

            _logger.LogInformation("Updated client: {ClientName} (ID: {ClientId})", existingClient.Name, id);

            var clientDto = new ClientResponseDto
            {
                Id = existingClient.Id,
                Name = existingClient.Name,
                Email = existingClient.Email,
                Company = existingClient.Company,
                Status = existingClient.Status,
                Notes = existingClient.Notes,
                CreatedAt = existingClient.CreatedAt,
                UpdatedAt = existingClient.UpdatedAt,
                CampaignCount = existingClient.CampaignCount,
                ActiveCampaigns = existingClient.ActiveCampaigns,
                EmailAccountCount = existingClient.EmailAccountCount
            };

            return Ok(new { success = true, data = clientDto, message = "Client updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating client {ClientId}", id);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete a client - Permanently deletes client from system. ⚠️ CANNOT BE UNDONE. Client must have no campaigns or email accounts. Pre-deletion validation performed.
    /// </summary>
    /// <param name="id">Client ID to delete</param>
    /// <returns>Confirmation message</returns>
    /// <response code="200">Client deleted successfully</response>
    /// <response code="400">Cannot delete client (has associated campaigns or email accounts)</response>
    /// <response code="404">Client not found</response>
    /// <example>
    /// Sample success response:
    /// {
    ///   "success": true,
    ///   "message": "Client deleted successfully"
    /// }
    /// 
    /// Sample error response (400):
    /// {
    ///   "success": false,
    ///   "message": "Cannot delete client with associated campaigns",
    ///   "details": {
    ///     "campaignCount": 5
    ///   }
    /// }
    /// </example>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ClientDeletionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteClient(string id)
    {
        try
        {
            // Only admins can delete clients
            if (!IsAdmin())
            {
                return Forbid();
            }

            var client = await _clientRepository.GetByIdAsync(id);
            if (client == null)
            {
                return NotFound(new { success = false, message = "Client not found" });
            }

            // Check if client has associated campaigns
            var campaigns = (await _campaignRepository.GetByClientIdAsync(id)).ToList();
            if (campaigns.Any())
            {
                return BadRequest(new { 
                    success = false,
                    message = "Cannot delete client with associated campaigns",
                    details = new { campaignCount = campaigns.Count }
                });
            }

            // Check if client has associated email accounts
            var emailAccounts = (await _emailAccountRepository.GetByClientIdAsync(id)).ToList();
            if (emailAccounts.Any())
            {
                return BadRequest(new { 
                    success = false,
                    message = "Cannot delete client with associated email accounts",
                    details = new { emailAccountCount = emailAccounts.Count }
                });
            }

            await _clientRepository.DeleteAsync(id);

            _logger.LogInformation("Deleted client: {ClientName} (ID: {ClientId})", client.Name, id);

            return Ok(new { success = true, message = "Client deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting client {ClientId}", id);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Search clients - Quick search across client name, email, and company with limit
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="limit">Maximum results to return (max: 50)</param>
    /// <returns>Matching clients in simplified format</returns>
    /// <response code="200">Search results retrieved successfully</response>
    /// <example>
    /// Sample response:
    /// {
    ///   "success": true,
    ///   "data": [
    ///     {
    ///       "id": "client-001",
    ///       "name": "Acme Corporation",
    ///       "email": "contact@acme.com",
    ///       "company": "Acme Corp",
    ///       "status": "active"
    ///     },
    ///     {
    ///       "id": "client-002",
    ///       "name": "TechStartup Inc",
    ///       "email": "hello@techstartup.io",
    ///       "company": "TechStartup Inc",
    ///       "status": "active"
    ///     }
    ///   ]
    /// }
    /// </example>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ClientSearchResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchClients(
        [FromQuery] string query = "", 
        [FromQuery] int limit = 10)
    {
        try
        {
            // Only admins can access client endpoints
            if (!IsAdmin())
            {
                return Forbid();
            }

            limit = Math.Clamp(limit, 1, 50);
            
            var allClients = (await _clientRepository.GetAllAsync()).ToList();

            var filteredClients = string.IsNullOrWhiteSpace(query) 
                ? allClients
                : allClients.Where(c => 
                    c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    (c.Email?.Contains(query, StringComparison.OrdinalIgnoreCase) == true) ||
                    (c.Company?.Contains(query, StringComparison.OrdinalIgnoreCase) == true))
                    .ToList();

            var result = filteredClients
                .OrderBy(c => c.Name)
                .Take(limit)
                .Select(c => new 
                { 
                    id = c.Id, 
                    name = c.Name, 
                    email = c.Email,
                    company = c.Company,
                    status = c.Status
                })
                .ToList();

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching clients with query: {Query}", query);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }


    /// <summary>
    /// Get client's campaigns - Retrieves paginated list of campaigns belonging to the specified client with pagination support.
    /// </summary>
    /// <param name="id">Client ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="limit">Items per page (default: 100, max: 100)</param>
    /// <returns>Paginated list of client's campaigns with metrics</returns>
    /// <response code="200">Client campaigns retrieved successfully</response>
    /// <response code="400">Invalid parameters</response>
    /// <response code="404">Client not found</response>
    /// <example>
    /// Sample response:
    /// {
    ///   "success": true,
    ///   "data": [
    ///     {
    ///       "id": "campaign-001",
    ///       "campaignId": 12345,
    ///       "name": "Q4 Product Launch",
    ///       "status": "Active",
    ///       "totalLeads": 1500,
    ///       "totalSent": 8750
    ///     }
    ///   ],
    ///   "pagination": {
    ///     "totalCount": 12,
    ///     "page": 1,
    ///     "limit": 100,
    ///     "totalPages": 1
    ///   }
    /// }
    /// </example>
    [HttpGet("{id}/campaigns")]
    [ProducesResponseType(typeof(ClientCampaignsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClientCampaigns(
        string id,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 100)
    {
        try
        {
            // Only admins can access client endpoints
            if (!IsAdmin())
            {
                return Forbid();
            }

            // Validate pagination parameters
            page = Math.Max(1, page);
            if (limit < 1)
            {
                return BadRequest(new { success = false, message = "Limit must be at least 1" });
            }
            if (limit > 100)
            {
                return BadRequest(new { success = false, message = "Limit cannot exceed 100" });
            }
            
            var client = await _clientRepository.GetByIdAsync(id);
            if (client == null)
            {
                return NotFound(new { success = false, message = "Client not found" });
            }

            var campaigns = (await _campaignRepository.GetByClientIdAsync(id)).ToList();

            // Apply pagination
            var totalCount = campaigns.Count;
            var paginatedCampaigns = campaigns
                .OrderByDescending(c => c.UpdatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToList();

            return Ok(new { 
                success = true, 
                data = paginatedCampaigns,
                pagination = new
                {
                    totalCount,
                    page,
                    limit,
                    totalPages = (int)Math.Ceiling(totalCount / (double)limit)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving campaigns for client {ClientId}", id);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get client's email accounts - Retrieves paginated list of email accounts belonging to the specified client with pagination support.
    /// </summary>
    /// <param name="id">Client ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="limit">Items per page (default: 100, max: 100)</param>
    /// <returns>Paginated list of client's email accounts with status info</returns>
    /// <response code="200">Client email accounts retrieved successfully</response>
    /// <response code="400">Invalid parameters</response>
    /// <response code="404">Client not found</response>
    /// <example>
    /// Sample response:
    /// {
    ///   "success": true,
    ///   "data": [
    ///     {
    ///       "id": 1001,
    ///       "email": "sender@acme.com",
    ///       "name": "John Sender",
    ///       "status": "Active",
    ///       "warmupEnabled": true
    ///     }
    ///   ],
    ///   "pagination": {
    ///     "totalCount": 25,
    ///     "page": 1,
    ///     "limit": 100,
    ///     "totalPages": 1
    ///   }
    /// }
    /// </example>
    [HttpGet("{id}/email-accounts")]
    [ProducesResponseType(typeof(ClientEmailAccountsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClientEmailAccounts(
        string id,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 100)
    {
        try
        {
            // Only admins can access client endpoints
            if (!IsAdmin())
            {
                return Forbid();
            }

            // Validate pagination parameters
            page = Math.Max(1, page);
            if (limit < 1)
            {
                return BadRequest(new { success = false, message = "Limit must be at least 1" });
            }
            if (limit > 100)
            {
                return BadRequest(new { success = false, message = "Limit cannot exceed 100" });
            }
            
            var client = await _clientRepository.GetByIdAsync(id);
            if (client == null)
            {
                return NotFound(new { success = false, message = "Client not found" });
            }

            var emailAccounts = (await _emailAccountRepository.GetByClientIdAsync(id)).ToList();

            // Apply pagination
            var totalCount = emailAccounts.Count;
            var paginatedEmailAccounts = emailAccounts
                .OrderByDescending(ea => ea.UpdatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToList();

            return Ok(new { 
                success = true, 
                data = paginatedEmailAccounts,
                pagination = new
                {
                    totalCount,
                    page,
                    limit,
                    totalPages = (int)Math.Ceiling(totalCount / (double)limit)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving email accounts for client {ClientId}", id);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Assign client to campaigns
    /// </summary>
    /// <param name="id">Client ID</param>
    /// <param name="request">Campaign assignment request</param>
    /// <returns>Assignment result</returns>
    [HttpPost("{id}/assign-campaigns")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignClientToCampaigns(string id, [FromBody] AssignCampaignsRequest request)
    {
        try
        {
            // Only admins can assign clients
            if (!IsAdmin())
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid request data", errors = ModelState });
            }

            // Verify client exists
            var client = await _clientRepository.GetByIdAsync(id);
            if (client == null)
            {
                return NotFound(new { success = false, message = "Client not found" });
            }

            int assignedCount = 0;
            var failedCampaigns = new List<string>();

            // Update each campaign
            foreach (var campaignId in request.CampaignIds)
            {
                try
                {
                    var campaign = await _campaignRepository.GetByIdAsync(campaignId);
                    if (campaign == null)
                    {
                        failedCampaigns.Add($"Campaign {campaignId} not found");
                        continue;
                    }

                    // Assign client to campaign
                    campaign.ClientId = id;
                    campaign.UpdatedAt = DateTime.UtcNow;

                    await _campaignRepository.UpdateAsync(campaign);
                    assignedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error assigning client {ClientId} to campaign {CampaignId}", id, campaignId);
                    failedCampaigns.Add($"Campaign {campaignId} assignment failed");
                }
            }

            var response = new
            {
                success = true,
                message = $"Client assigned to {assignedCount} of {request.CampaignIds.Count} campaigns",
                data = new
                {
                    clientId = id,
                    assignedCount = assignedCount,
                    totalRequested = request.CampaignIds.Count,
                    failedCampaigns = failedCampaigns
                }
            };

            if (failedCampaigns.Any())
            {
                _logger.LogWarning("Client assignment to campaigns completed with some failures: {FailedCount} out of {TotalCount}", 
                    failedCampaigns.Count, request.CampaignIds.Count);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning client {ClientId} to campaigns", id);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Assign client to email accounts
    /// </summary>
    /// <param name="id">Client ID</param>
    /// <param name="request">Email account assignment request</param>
    /// <returns>Assignment result</returns>
    [HttpPost("{id}/assign-email-accounts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignClientToEmailAccounts(string id, [FromBody] AssignEmailAccountsRequest request)
    {
        try
        {
            // Only admins can assign clients
            if (!IsAdmin())
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid request data", errors = ModelState });
            }

            // Verify client exists
            var client = await _clientRepository.GetByIdAsync(id);
            if (client == null)
            {
                return NotFound(new { success = false, message = "Client not found" });
            }

            int assignedCount = 0;
            var failedEmailAccounts = new List<string>();

            // Update each email account
            foreach (var emailAccountIdStr in request.EmailAccountIds)
            {
                try
                {
                    if (!long.TryParse(emailAccountIdStr, out var emailAccountId))
                    {
                        failedEmailAccounts.Add($"Email account {emailAccountIdStr} has invalid ID format");
                        continue;
                    }

                    var emailAccount = await _emailAccountRepository.GetByIdAsync(emailAccountId);
                    if (emailAccount == null)
                    {
                        failedEmailAccounts.Add($"Email account {emailAccountIdStr} not found");
                        continue;
                    }

                    // Assign client to email account
                    emailAccount.ClientId = id;
                    emailAccount.UpdatedAt = DateTime.UtcNow;

                    await _emailAccountRepository.UpdateAsync(emailAccount);
                    assignedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error assigning client {ClientId} to email account {EmailAccountId}", id, emailAccountIdStr);
                    failedEmailAccounts.Add($"Email account {emailAccountIdStr} assignment failed");
                }
            }

            var response = new
            {
                success = true,
                message = $"Client assigned to {assignedCount} of {request.EmailAccountIds.Count} email accounts",
                data = new
                {
                    clientId = id,
                    assignedCount = assignedCount,
                    totalRequested = request.EmailAccountIds.Count,
                    failedEmailAccounts = failedEmailAccounts
                }
            };

            if (failedEmailAccounts.Any())
            {
                _logger.LogWarning("Client assignment to email accounts completed with some failures: {FailedCount} out of {TotalCount}", 
                    failedEmailAccounts.Count, request.EmailAccountIds.Count);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning client {ClientId} to email accounts", id);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get client statistics dashboard - Retrieves engagement metrics, reply timing, and performance stats for all clients
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20)</param>
    /// <param name="sortBy">Field to sort by (default: name)</param>
    /// <param name="sortDescending">Sort in descending order (default: false)</param>
    /// <param name="clientIds">Filter by specific client IDs</param>
    /// <param name="startDate">Filter statistics from this date (ISO format)</param>
    /// <param name="endDate">Filter statistics to this date (ISO format)</param>
    /// <param name="clientStatus">Filter by client status (active, inactive)</param>
    /// <returns>Client statistics with reply timing and engagement metrics</returns>
    /// <response code="200">Client statistics retrieved successfully</response>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ClientStatsCollectionResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClientStats(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "name",
        [FromQuery] bool sortDescending = false,
        [FromQuery] string[]? clientIds = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? clientStatus = null)
    {
        try
        {
            // Get user's assigned client IDs for filtering
            var assignedClientIds = await GetUserAssignedClientIds();
            
            // If user has no assigned clients, return empty result
            if (assignedClientIds != null && !assignedClientIds.Any())
            {
                return Ok(new ClientStatsCollectionResponse
                {
                    Clients = new List<ClientStatsResponse>(),
                    TotalCount = 0,
                    GeneratedAt = DateTime.UtcNow,
                    Pagination = new PaginationInfo
                    {
                        Page = page,
                        PageSize = pageSize,
                        TotalPages = 0,
                        HasNextPage = false,
                        HasPreviousPage = false
                    }
                });
            }

            // For regular users, filter to their assigned clients
            string[]? finalClientIds = null;
            if (assignedClientIds != null) // null means admin (no filtering)
            {
                // If clientIds parameter is provided, intersect with assigned clients
                finalClientIds = clientIds?.Intersect(assignedClientIds).ToArray() ?? assignedClientIds.ToArray();
            }
            else
            {
                // Admin user - use provided clientIds if any
                finalClientIds = clientIds;
            }

            var stats = await _clientStatsService.GetAllClientStatsAsync(page, pageSize, sortBy, sortDescending, finalClientIds, startDate, endDate, clientStatus, null);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving client statistics");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get specific client statistics - Retrieves detailed engagement metrics and reply timing for a specific client
    /// </summary>
    /// <param name="id">Client ID</param>
    /// <returns>Client statistics with reply timing and engagement metrics</returns>
    /// <response code="200">Client statistics retrieved successfully</response>
    /// <response code="404">Client not found</response>
    [HttpGet("{id}/stats")]
    [ProducesResponseType(typeof(ClientStatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClientStats(string id)
    {
        try
        {
            // Get user's assigned client IDs for filtering
            var assignedClientIds = await GetUserAssignedClientIds();
            
            // Check if user has access to this client
            if (assignedClientIds != null && !assignedClientIds.Contains(id))
            {
                return Forbid();
            }

            var stats = await _clientStatsService.GetClientStatsAsync(id);
            if (stats == null)
            {
                return NotFound(new { success = false, message = "Client not found" });
            }

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving client statistics for client {ClientId}", id);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }
}

/// <summary>
/// Request model for creating a new client
/// </summary>
public class CreateClientRequest
{
    /// <summary>
    /// Client name (required)
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, ErrorMessage = "Name must be less than 200 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Client email address (optional)
    /// </summary>
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(200, ErrorMessage = "Email must be less than 200 characters")]
    public string? Email { get; set; }

    /// <summary>
    /// Client company name (optional)
    /// </summary>
    [StringLength(200, ErrorMessage = "Company must be less than 200 characters")]
    public string? Company { get; set; }

    /// <summary>
    /// Client status (default: active)
    /// </summary>
    [StringLength(50, ErrorMessage = "Status must be less than 50 characters")]
    public string? Status { get; set; }

    /// <summary>
    /// Client color in hex format (optional, random color assigned if not provided)
    /// </summary>
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex color")]
    public string? Color { get; set; }

    /// <summary>
    /// Client notes (optional)
    /// </summary>
    [StringLength(1000, ErrorMessage = "Notes must be less than 1000 characters")]
    public string? Notes { get; set; }
}

/// <summary>
/// Response DTO for client data (excludes internal fields like Color)
/// </summary>
public class ClientResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Company { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int CampaignCount { get; set; }
    public int ActiveCampaigns { get; set; }
    public int EmailAccountCount { get; set; }
}

/// <summary>
/// Request model for assigning client to campaigns
/// </summary>
public class AssignCampaignsRequest
{
    /// <summary>
    /// List of campaign IDs to assign the client to
    /// </summary>
    [Required(ErrorMessage = "Campaign IDs are required")]
    [MinLength(1, ErrorMessage = "At least one campaign ID is required")]
    public List<string> CampaignIds { get; set; } = new List<string>();
}

/// <summary>
/// Request model for assigning client to email accounts
/// </summary>
public class AssignEmailAccountsRequest
{
    /// <summary>
    /// List of email account IDs to assign the client to
    /// </summary>
    [Required(ErrorMessage = "Email account IDs are required")]
    [MinLength(1, ErrorMessage = "At least one email account ID is required")]
    public List<string> EmailAccountIds { get; set; } = new List<string>();
}

/// <summary>
/// Request model for updating an existing client
/// </summary>
public class UpdateClientRequest
{
    /// <summary>
    /// Client name (required)
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, ErrorMessage = "Name must be less than 200 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Client email address (optional)
    /// </summary>
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(200, ErrorMessage = "Email must be less than 200 characters")]
    public string? Email { get; set; }

    /// <summary>
    /// Client company name (optional)
    /// </summary>
    [StringLength(200, ErrorMessage = "Company must be less than 200 characters")]
    public string? Company { get; set; }

    /// <summary>
    /// Client status (optional)
    /// </summary>
    [StringLength(50, ErrorMessage = "Status must be less than 50 characters")]
    public string? Status { get; set; }

    /// <summary>
    /// Client color in hex format (optional)
    /// </summary>
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be a valid hex color")]
    public string? Color { get; set; }

    /// <summary>
    /// Client notes (optional)
    /// </summary>
    [StringLength(1000, ErrorMessage = "Notes must be less than 1000 characters")]
    public string? Notes { get; set; }
}

