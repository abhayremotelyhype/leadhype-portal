using LeadHype.Api.Core.Database.Repositories;
using LeadHype.Api.Core.Database.Models;
using LeadHype.Api.Core.Models;
using LeadHype.Api.Core.Models.Auth;
using LeadHype.Api.Models;
using LeadHype.Api.Models.UI;
using LeadHype.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LeadHype.Api.Controllers;

[ApiController]
[Route("api/clients")]
[Authorize]
public class ClientController : ControllerBase
{
    private readonly IClientRepository _clientRepository;
    private readonly ICampaignRepository _campaignRepository;
    private readonly IEmailAccountRepository _emailAccountRepository;
    private readonly ILogger<ClientController> _logger;
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

    public ClientController(
        IClientRepository clientRepository,
        ICampaignRepository campaignRepository,
        IEmailAccountRepository emailAccountRepository,
        ILogger<ClientController> logger, 
        IAuthService authService)
    {
        _clientRepository = clientRepository;
        _campaignRepository = campaignRepository;
        _emailAccountRepository = emailAccountRepository;
        _logger = logger;
        _authService = authService;
    }

    private string GetRandomColor()
    {
        var random = new Random();
        return ClientColors[random.Next(ClientColors.Length)];
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

    private IEnumerable<Client> ApplySorting(IEnumerable<Client> clients, string? sortBy, string? sortDirection)
    {
        var isDescending = sortDirection?.ToLower() == "desc";
        
        return sortBy?.ToLower() switch
        {
            "name" => isDescending 
                ? clients.OrderByDescending(c => c.Name) 
                : clients.OrderBy(c => c.Name),
            "campaigncount" => isDescending 
                ? clients.OrderByDescending(c => c.CampaignCount) 
                : clients.OrderBy(c => c.CampaignCount),
            "activecampaigns" => isDescending 
                ? clients.OrderByDescending(c => c.ActiveCampaigns) 
                : clients.OrderBy(c => c.ActiveCampaigns),
            "emailaccountcount" => isDescending 
                ? clients.OrderByDescending(c => c.EmailAccountCount) 
                : clients.OrderBy(c => c.EmailAccountCount),
            "email" => isDescending 
                ? clients.OrderByDescending(c => c.Email ?? "") 
                : clients.OrderBy(c => c.Email ?? ""),
            "company" => isDescending 
                ? clients.OrderByDescending(c => c.Company ?? "") 
                : clients.OrderBy(c => c.Company ?? ""),
            "status" => isDescending 
                ? clients.OrderByDescending(c => c.Status) 
                : clients.OrderBy(c => c.Status),
            "createdat" => isDescending 
                ? clients.OrderByDescending(c => c.CreatedAt) 
                : clients.OrderBy(c => c.CreatedAt),
            "updatedat" => isDescending 
                ? clients.OrderByDescending(c => c.UpdatedAt) 
                : clients.OrderBy(c => c.UpdatedAt),
            // Default sorting by creation date (oldest first)
            _ => clients.OrderBy(c => c.CreatedAt)
        };
    }

    [HttpGet]
    public async Task<IActionResult> GetClients(
        [FromQuery] int page = 1, 
        [FromQuery] int limit = 50, 
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = "asc",
        [FromQuery] string? sortMode = null)
    {
        try
        {
            // Get user's assigned client IDs for filtering
            var assignedClientIds = await GetUserAssignedClientIds();
            
            var clients = (await _clientRepository.GetAllAsync()).ToList();

            // Apply user-based filtering first
            if (assignedClientIds != null) // null means admin (no filtering)
            {
                if (!assignedClientIds.Any())
                {
                    // User has no assigned clients, return empty result
                    return Ok(new 
                    { 
                        clients = new List<Client>(), 
                        totalCount = 0,
                        page,
                        limit,
                        totalPages = 0
                    });
                }
                
                // Filter to only assigned clients
                clients = clients.Where(c => assignedClientIds.Contains(c.Id)).ToList();
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                clients = clients.Where(c => 
                    c.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (c.Email?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                    (c.Company?.Contains(search, StringComparison.OrdinalIgnoreCase) == true))
                    .ToList();
            }

            // Calculate additional stats for each client first (needed for sorting by count fields)
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

            // Apply dynamic sorting
            enrichedClients = ApplySorting(enrichedClients, sortBy, sortDirection).ToList();

            // Apply pagination
            var totalCount = enrichedClients.Count;
            var paginatedClients = enrichedClients
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToList();

            return Ok(new 
            { 
                clients = paginatedClients, 
                totalCount,
                page,
                limit,
                totalPages = (int)Math.Ceiling(totalCount / (double)limit)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving clients");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetClient(string id)
    {
        try
        {
            // Get user's assigned client IDs for filtering
            var assignedClientIds = await GetUserAssignedClientIds();
            
            var client = await _clientRepository.GetByIdAsync(id);
            if (client == null)
            {
                return NotFound(new { message = "Client not found" });
            }

            // Check if user has access to this client
            if (assignedClientIds != null && !assignedClientIds.Contains(id))
            {
                return Forbid("You don't have access to this client");
            }

            // Enrich with campaign and email account data
            var campaigns = (await _campaignRepository.GetByClientIdAsync(client.Id)).ToList();
            client.CampaignCount = campaigns.Count;
            client.ActiveCampaigns = campaigns.Count(c => c.Status?.ToLower() == "active");

            var emailAccounts = (await _emailAccountRepository.GetByClientIdAsync(client.Id)).ToList();
            client.EmailAccountCount = emailAccounts.Count;

            return Ok(client);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving client {ClientId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateClient([FromBody] CreateClientRequest request)
    {
        try
        {
            // Only admins can create clients
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != UserRoles.Admin)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
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

            return CreatedAtAction(nameof(GetClient), new { id = client.Id }, client);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating client");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateClient(string id, [FromBody] UpdateClientRequest request)
    {
        try
        {
            // Only admins can update clients
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != UserRoles.Admin)
            {
                return Forbid();
            }

            var existingClient = await _clientRepository.GetByIdAsync(id);
            if (existingClient == null)
            {
                return NotFound(new { message = "Client not found" });
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

            return Ok(existingClient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating client {ClientId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteClient(string id)
    {
        try
        {
            // Only admins can delete clients
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != UserRoles.Admin)
            {
                return Forbid();
            }

            var client = await _clientRepository.GetByIdAsync(id);
            if (client == null)
            {
                return NotFound(new { message = "Client not found" });
            }

            // Check if client has associated campaigns
            var campaigns = (await _campaignRepository.GetByClientIdAsync(id)).ToList();
            if (campaigns.Any())
            {
                return BadRequest(new { 
                    message = "Cannot delete client with associated campaigns",
                    campaignCount = campaigns.Count 
                });
            }

            // Check if client has associated email accounts
            var emailAccounts = (await _emailAccountRepository.GetByClientIdAsync(id)).ToList();
            if (emailAccounts.Any())
            {
                return BadRequest(new { 
                    message = "Cannot delete client with associated email accounts",
                    emailAccountCount = emailAccounts.Count 
                });
            }

            await _clientRepository.DeleteAsync(id);

            _logger.LogInformation("Deleted client: {ClientName} (ID: {ClientId})", client.Name, id);

            return Ok(new { message = "Client deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting client {ClientId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchClients([FromQuery] string query = "", [FromQuery] int limit = 10)
    {
        try
        {
            // Get user's assigned client IDs for filtering
            var assignedClientIds = await GetUserAssignedClientIds();
            
            var allClients = (await _clientRepository.GetAllAsync()).ToList();

            // Apply user-based filtering first
            if (assignedClientIds != null) // null means admin (no filtering)
            {
                if (!assignedClientIds.Any())
                {
                    return Ok(new List<Client>()); // No assigned clients, return empty
                }
                allClients = allClients.Where(c => assignedClientIds.Contains(c.Id)).ToList();
            }

            var filteredClients = string.IsNullOrWhiteSpace(query) 
                ? allClients
                : allClients.Where(c => 
                    c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    (c.Email?.Contains(query, StringComparison.OrdinalIgnoreCase) == true) ||
                    (c.Company?.Contains(query, StringComparison.OrdinalIgnoreCase) == true))
                    .ToList();

            // Enrich with campaign and email account counts
            foreach (var client in filteredClients)
            {
                var campaigns = (await _campaignRepository.GetByClientIdAsync(client.Id)).ToList();
                client.CampaignCount = campaigns.Count;
                client.ActiveCampaigns = campaigns.Count(c => c.Status?.ToLower() == "active");

                var emailAccounts = (await _emailAccountRepository.GetByClientIdAsync(client.Id)).ToList();
                client.EmailAccountCount = emailAccounts.Count;
            }

            var result = filteredClients.Take(limit).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching clients with query: {Query}", query);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{id}/campaigns")]
    public async Task<IActionResult> GetClientCampaigns(string id)
    {
        try
        {
            // Get user's assigned client IDs for filtering
            var assignedClientIds = await GetUserAssignedClientIds();
            
            var client = await _clientRepository.GetByIdAsync(id);
            if (client == null)
            {
                return NotFound(new { message = "Client not found" });
            }

            // Check if user has access to this client
            if (assignedClientIds != null && !assignedClientIds.Contains(id))
            {
                return Forbid("You don't have access to this client");
            }

            var campaigns = await _campaignRepository.GetByClientIdAsync(id);

            return Ok(campaigns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving campaigns for client {ClientId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("assign-color")]
    public async Task<IActionResult> AssignColorsToClients()
    {
        try
        {
            var clients = (await _clientRepository.GetAllAsync()).ToList();
            var random = new Random();

            foreach (var client in clients.Where(c => string.IsNullOrEmpty(c.Color) || c.Color == "#3B82F6"))
            {
                client.Color = ClientColors[random.Next(ClientColors.Length)];
                client.UpdatedAt = DateTime.UtcNow;
                await _clientRepository.UpdateAsync(client);
            }

            _logger.LogInformation("Assigned colors to {ClientCount} clients", clients.Count);

            return Ok(new { message = $"Assigned colors to {clients.Count} clients" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning colors to clients");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{id}/email-accounts")]
    public async Task<IActionResult> GetClientEmailAccounts(string id)
    {
        try
        {
            // Get user's assigned client IDs for filtering
            var assignedClientIds = await GetUserAssignedClientIds();
            
            var client = await _clientRepository.GetByIdAsync(id);
            if (client == null)
            {
                return NotFound(new { message = "Client not found" });
            }

            // Check if user has access to this client
            if (assignedClientIds != null && !assignedClientIds.Contains(id))
            {
                return Forbid("You don't have access to this client");
            }

            var emailAccounts = await _emailAccountRepository.GetByClientIdAsync(id);

            return Ok(emailAccounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving email accounts for client {ClientId}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("dropdown")]
    public async Task<IActionResult> GetClientsForDropdown([FromQuery] string? search = null)
    {
        try
        {
            // Get user's assigned client IDs for filtering
            var assignedClientIds = await GetUserAssignedClientIds();
            
            var clients = (await _clientRepository.GetAllAsync()).ToList();

            // Apply user-based filtering first
            if (assignedClientIds != null) // null means admin (no filtering)
            {
                if (!assignedClientIds.Any())
                {
                    return Ok(new List<object>()); // No assigned clients, return empty
                }
                clients = clients.Where(c => assignedClientIds.Contains(c.Id)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                clients = clients.Where(c => 
                    c.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var dropdownItems = clients
                .OrderBy(c => c.Name)
                .Select(c => new { id = c.Id, name = c.Name, color = c.Color })
                .ToList();

            return Ok(dropdownItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving clients for dropdown");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("list")]
    public async Task<ActionResult<PaginatedResponse<ClientListItem>>> GetClientList(
        [FromQuery] string? search = null,
        [FromQuery] int limit = 1000, // Default high limit for backward compatibility
        [FromQuery] int offset = 0,
        [FromQuery] string? filterByUserId = null)
    {
        try
        {
            // Get user's assigned client IDs for filtering
            var assignedClientIds = await GetUserAssignedClientIds();
            
            var clients = (await _clientRepository.GetAllAsync()).ToList();

            // Apply user-based filtering
            if (assignedClientIds != null) // null means admin (no filtering)
            {
                if (!assignedClientIds.Any())
                {
                    // User has no assigned clients, return empty result
                    return Ok(new PaginatedResponse<ClientListItem>
                    {
                        Data = new List<ClientListItem>(),
                        CurrentPage = 1,
                        PageSize = limit,
                        TotalCount = 0,
                        TotalPages = 0,
                        HasPrevious = false,
                        HasNext = false
                    });
                }
                clients = clients.Where(c => assignedClientIds.Contains(c.Id)).ToList();
            }

            // Apply additional user filter if specified (for cascading filters)
            if (!string.IsNullOrWhiteSpace(filterByUserId))
            {
                var targetUser = await _authService.GetUserByIdAsync(filterByUserId);
                if (targetUser != null && targetUser.AssignedClientIds != null && targetUser.AssignedClientIds.Any())
                {
                    // Filter to only clients assigned to the specified user
                    clients = clients.Where(c => targetUser.AssignedClientIds.Contains(c.Id)).ToList();
                }
                else
                {
                    // If user not found or has no assigned clients, return empty result
                    return Ok(new PaginatedResponse<ClientListItem>
                    {
                        Data = new List<ClientListItem>(),
                        CurrentPage = 1,
                        PageSize = limit,
                        TotalCount = 0,
                        TotalPages = 0,
                        HasPrevious = false,
                        HasNext = false
                    });
                }
            }

            // Apply search filter
            var filteredClients = clients
                .Where(c => string.IsNullOrWhiteSpace(search) || 
                           c.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.Name)
                .ToList();
            
            // Calculate pagination
            var totalCount = filteredClients.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)limit);
            var currentPage = (offset / limit) + 1;
            
            // Apply pagination
            var paginatedClients = filteredClients
                .Skip(offset)
                .Take(limit)
                .Select(c => new ClientListItem { Id = c.Id, Name = c.Name })
                .ToList();
            
            return Ok(new PaginatedResponse<ClientListItem>
            {
                Data = paginatedClients,
                CurrentPage = currentPage,
                PageSize = limit,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasPrevious = offset > 0,
                HasNext = (offset + limit) < totalCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching client list");
            return StatusCode(500, new { message = "Error fetching client list" });
        }
    }
}

// Supporting DTOs
public class CreateClientRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Company { get; set; }
    public string? Status { get; set; }
    public string? Color { get; set; }
    public string? Notes { get; set; }
}

public class UpdateClientRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Company { get; set; }
    public string? Status { get; set; }
    public string? Color { get; set; }
    public string? Notes { get; set; }
}

public class ClientListItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}