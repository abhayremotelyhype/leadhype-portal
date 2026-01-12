using LeadHype.Api.Core.Database.Repositories;
using LeadHype.Api.Core.Models;
using LeadHype.Api.Core.Models.API;
using LeadHype.Api.Core.Models.Auth;
using LeadHype.Api.Core.Models.Frontend;
using LeadHype.Api.Core.Models.API.Responses;
using LeadHype.Api.Core.Services.Analytics;
using LeadHype.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LeadHype.Api.Controllers.V1;

/// <summary>
/// Users management API for external admin access
/// </summary>
[ApiController]
[Route("api/v1/users")]
[Authorize(AuthenticationSchemes = "ApiKey")]
[Authorize(Roles = UserRoles.Admin)]
[Produces("application/json")]
[Tags("Users V1")]
public class UsersV1Controller : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserRepository _userRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IUserStatsService _userStatsService;
    private readonly ILogger<UsersV1Controller> _logger;

    public UsersV1Controller(
        IAuthService authService,
        IUserRepository userRepository,
        IClientRepository clientRepository,
        IUserStatsService userStatsService,
        ILogger<UsersV1Controller> logger)
    {
        _authService = authService;
        _userRepository = userRepository;
        _clientRepository = clientRepository;
        _userStatsService = userStatsService;
        _logger = logger;
    }

    /// <summary>
    /// Assign or update clients for a user (non-admin users only)
    /// </summary>
    /// <param name="request">Client assignment request</param>
    /// <returns>Assignment result with updated user information</returns>
    /// <remarks>
    /// This endpoint can only assign clients to users with the "User" role. 
    /// Admin users have access to all clients by default and cannot be assigned specific clients.
    /// To remove all client assignments, pass an empty clientIds array.
    /// </remarks>
    /// <response code="200">Clients successfully assigned to user</response>
    /// <response code="400">Invalid request data or attempting to assign clients to admin user</response>
    /// <response code="401">Unauthorized - invalid API key</response>
    /// <response code="403">Forbidden - admin access required</response>
    /// <response code="404">User not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("assign-clients")]
    [ProducesResponseType(typeof(AssignClientsToUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AssignClientsToUserResponse>> AssignClientsToUser([FromBody] AssignClientsToUserRequest request)
    {
        try
        {
            _logger.LogInformation("Admin assigning clients to user {UserId}. Client count: {ClientCount}", 
                request.UserId, request.ClientIds.Count);

            // Validate the user exists
            var user = await _authService.GetUserByIdAsync(request.UserId);
            if (user == null)
            {
                _logger.LogWarning("Attempted to assign clients to non-existent user {UserId}", request.UserId);
                return NotFound(new AssignClientsToUserResponse
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            // Get current admin user ID for logging and protection checks
            var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("Admin user {AdminUserId} assigning clients to user {UserId}", 
                adminUserId, request.UserId);

            // Prevent admins from modifying their own assignments (to prevent lockout)
            if (adminUserId == request.UserId)
            {
                _logger.LogWarning("Admin user {AdminUserId} attempted to modify their own client assignments", adminUserId);
                return BadRequest(new AssignClientsToUserResponse
                {
                    Success = false,
                    Message = "You cannot modify your own client assignments"
                });
            }

            // Prevent assigning clients to admin users (they have access to all clients)
            if (user.Role == UserRoles.Admin)
            {
                _logger.LogWarning("Attempted to assign clients to admin user {UserId} ({Username})", 
                    request.UserId, user.Username);
                return BadRequest(new AssignClientsToUserResponse
                {
                    Success = false,
                    Message = $"Cannot assign clients to admin users. Admin users have access to all clients by default."
                });
            }

            // Validate that all client IDs exist (if any are provided)
            var validClientIds = new List<string>();
            var invalidClientIds = new List<string>();

            if (request.ClientIds.Any())
            {
                var clientsFromDb = await _clientRepository.GetAllAsync();
                var existingClientIds = clientsFromDb.Select(c => c.Id).ToHashSet();

                foreach (var clientId in request.ClientIds.Distinct())
                {
                    if (existingClientIds.Contains(clientId))
                    {
                        validClientIds.Add(clientId);
                    }
                    else
                    {
                        invalidClientIds.Add(clientId);
                    }
                }

                if (invalidClientIds.Any())
                {
                    _logger.LogWarning("Invalid client IDs provided: {InvalidClientIds}", string.Join(", ", invalidClientIds));
                    return BadRequest(new AssignClientsToUserResponse
                    {
                        Success = false,
                        Message = $"Invalid client IDs: {string.Join(", ", invalidClientIds)}"
                    });
                }
            }

            // Update user's assigned clients
            var updateRequest = new UpdateUserRequest
            {
                AssignedClientIds = validClientIds
            };

            var success = await _authService.UpdateUserAsync(request.UserId, updateRequest);
            
            if (!success)
            {
                _logger.LogError("Failed to update user {UserId} with client assignments", request.UserId);
                return StatusCode(500, new AssignClientsToUserResponse
                {
                    Success = false,
                    Message = "Failed to update user client assignments"
                });
            }

            // Get updated user information
            var updatedUser = await _authService.GetUserByIdAsync(request.UserId);
            if (updatedUser == null)
            {
                _logger.LogError("User {UserId} not found after successful update", request.UserId);
                return StatusCode(500, new AssignClientsToUserResponse
                {
                    Success = false,
                    Message = "Failed to retrieve updated user information"
                });
            }

            // Get client details for response
            var allClients = await _clientRepository.GetAllAsync();
            var assignedClients = allClients
                .Where(c => updatedUser.AssignedClientIds?.Contains(c.Id) == true)
                .Select(c => new AssignedClientInfo
                {
                    Id = c.Id,
                    Name = c.Name,
                    Color = c.Color
                })
                .ToList();

            var response = new AssignClientsToUserResponse
            {
                Success = true,
                Message = validClientIds.Any() 
                    ? $"Successfully assigned {validClientIds.Count} client(s) to user {updatedUser.Username}"
                    : $"Successfully removed all client assignments from user {updatedUser.Username}",
                User = new UserWithClientsInfo
                {
                    Id = updatedUser.Id,
                    Email = updatedUser.Email,
                    Username = updatedUser.Username,
                    FirstName = updatedUser.FirstName,
                    LastName = updatedUser.LastName,
                    Role = updatedUser.Role,
                    IsActive = updatedUser.IsActive,
                    AssignedClientIds = updatedUser.AssignedClientIds ?? new List<string>(),
                    AssignedClients = assignedClients
                }
            };

            _logger.LogInformation("Successfully assigned {ClientCount} clients to user {UserId} ({Username})", 
                validClientIds.Count, updatedUser.Id, updatedUser.Username);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning clients to user {UserId}", request.UserId);
            return StatusCode(500, new AssignClientsToUserResponse
            {
                Success = false,
                Message = "An internal error occurred while assigning clients to user"
            });
        }
    }

    /// <summary>
    /// Get user information with client assignments
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>User information with client details (all clients for admin users, assigned clients for regular users)</returns>
    /// <remarks>
    /// For admin users, this returns all available clients since admins have access to everything.
    /// For regular users, this returns only their specifically assigned clients.
    /// </remarks>
    /// <response code="200">User information retrieved successfully</response>
    /// <response code="401">Unauthorized - invalid API key</response>
    /// <response code="403">Forbidden - admin access required</response>
    /// <response code="404">User not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{userId}/clients")]
    [ProducesResponseType(typeof(UserWithClientsInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserWithClientsInfo>> GetUserWithClients(string userId)
    {
        try
        {
            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Get client details
            var allClients = await _clientRepository.GetAllAsync();
            
            // For admin users, return all clients; for regular users, return only assigned clients
            var assignedClients = user.Role == UserRoles.Admin
                ? allClients.Select(c => new AssignedClientInfo
                  {
                      Id = c.Id,
                      Name = c.Name,
                      Color = c.Color
                  }).ToList()
                : allClients
                  .Where(c => user.AssignedClientIds?.Contains(c.Id) == true)
                  .Select(c => new AssignedClientInfo
                  {
                      Id = c.Id,
                      Name = c.Name,
                      Color = c.Color
                  })
                  .ToList();
            
            // For admin users, assignedClientIds should reflect all clients
            var effectiveAssignedClientIds = user.Role == UserRoles.Admin
                ? allClients.Select(c => c.Id).ToList()
                : user.AssignedClientIds ?? new List<string>();

            var userInfo = new UserWithClientsInfo
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                IsActive = user.IsActive,
                AssignedClientIds = effectiveAssignedClientIds,
                AssignedClients = assignedClients
            };

            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId} with clients", userId);
            return StatusCode(500, new { message = "An internal error occurred" });
        }
    }

    /// <summary>
    /// Remove all client assignments from a user (non-admin users only)
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>Result of the operation</returns>
    /// <remarks>
    /// This endpoint can only remove client assignments from users with the "User" role.
    /// Admin users cannot have their client access modified as they have access to all clients by default.
    /// </remarks>
    /// <response code="200">All client assignments removed successfully</response>
    /// <response code="400">Invalid request - cannot remove assignments from admin users or own assignments</response>
    /// <response code="401">Unauthorized - invalid API key</response>
    /// <response code="403">Forbidden - admin access required</response>
    /// <response code="404">User not found</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete("{userId}/clients")]
    [ProducesResponseType(typeof(AssignClientsToUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AssignClientsToUserResponse>> RemoveAllClientAssignments(string userId)
    {
        try
        {
            // Reuse the assign clients logic with empty client list
            var request = new AssignClientsToUserRequest
            {
                UserId = userId,
                ClientIds = new List<string>()
            };

            var result = await AssignClientsToUser(request);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing all client assignments from user {UserId}", userId);
            return StatusCode(500, new AssignClientsToUserResponse
            {
                Success = false,
                Message = "An internal error occurred while removing client assignments"
            });
        }
    }

    /// <summary>
    /// Get user statistics and analytics (admin only)
    /// </summary>
    /// <param name="request">User statistics request with filtering and pagination options</param>
    /// <returns>User statistics with summary metrics and paginated user list</returns>
    /// <remarks>
    /// This endpoint provides comprehensive user analytics including:
    /// - Summary statistics (total users, active/inactive counts, role distribution)
    /// - Paginated user list with activity metrics
    /// - Client assignment information
    /// - API key usage statistics
    /// - Login activity tracking
    /// 
    /// Available filters:
    /// - Role: "All", "Admin", "User"
    /// - Status: "All", "Active", "Inactive"
    /// - Search: Username, email, first name, or last name
    /// 
    /// Sorting options: Username, Email, CreatedAt, LastLoginAt, Role
    /// </remarks>
    /// <response code="200">User statistics retrieved successfully</response>
    /// <response code="401">Unauthorized - invalid API key</response>
    /// <response code="403">Forbidden - admin access required</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("stats")]
    [ProducesResponseType(typeof(UserStatsCollectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserStatsCollectionResponse>> GetUserStats([FromBody] UserStatsRequest request)
    {
        try
        {
            _logger.LogInformation("Admin requesting user statistics - Page: {Page}, PageSize: {PageSize}, Role: {Role}, Status: {Status}",
                request.Page, request.PageSize, request.RoleFilter, request.StatusFilter);

            var stats = await _userStatsService.GetAllUserStatsAsync(
                page: request.Page,
                pageSize: request.PageSize,
                sortBy: request.SortBy ?? "username",
                sortDescending: request.SortDescending,
                roleFilter: request.RoleFilter,
                startDate: null, // Can be added later for date filtering
                endDate: null,   // Can be added later for date filtering
                statusFilter: request.StatusFilter,
                searchQuery: request.SearchQuery
            );

            _logger.LogInformation("Successfully retrieved user statistics - {UserCount} users returned (page {Page} of {TotalPages})",
                stats.Users.Count, stats.Pagination.Page, stats.Pagination.TotalPages);

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user statistics");
            return StatusCode(500, new { message = "An internal error occurred while retrieving user statistics" });
        }
    }
}