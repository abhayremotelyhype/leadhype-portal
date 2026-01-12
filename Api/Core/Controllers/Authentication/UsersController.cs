using LeadHype.Api.Core.Models.Auth;
using LeadHype.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using LeadHype.Api.Core.Database.Repositories;
using LeadHype.Api.Core.Models;
using LeadHype.Api.Models;

namespace LeadHype.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = UserRoles.Admin)]
    public class UsersController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserRepository _userRepository;
        private readonly IClientRepository _clientRepository;

        public UsersController(IAuthService authService, IUserRepository userRepository, IClientRepository clientRepository)
        {
            _authService = authService;
            _userRepository = userRepository;
            _clientRepository = clientRepository;
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        }

        private bool IsProtectedUser(Api.Core.Models.Auth.User user, string? currentUserId)
        {
            // Users cannot delete/modify themselves (prevents lockout)
            if (currentUserId != null && user.Id == currentUserId)
            {
                return true;
            }

            // Protect all admin users to maintain system integrity
            // In the future, this could be extended to check for a specific "super admin" role
            return user.Role == UserRoles.Admin;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _authService.GetUsersAsync();
            var userDtos = users.Select(u => new UserInfo
            {
                Id = u.Id,
                Email = u.Email,
                Username = u.Username,
                Role = u.Role,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsActive = u.IsActive,
                AssignedClientIds = u.AssignedClientIds,
                LastLoginAt = u.LastLoginAt
            }).ToList();

            return Ok(userDtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            var user = await _authService.GetUserByIdAsync(id);
            
            if (user == null)
            {
                return NotFound();
            }

            return Ok(new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                Role = user.Role,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive,
                AssignedClientIds = user.AssignedClientIds,
                LastLoginAt = user.LastLoginAt
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            var user = await _authService.CreateUserAsync(request);
            
            if (user == null)
            {
                return BadRequest(new { message = "User with this email already exists" });
            }

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                Role = user.Role,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive,
                AssignedClientIds = user.AssignedClientIds,
                LastLoginAt = user.LastLoginAt
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] Api.Services.UpdateUserRequest request)
        {
            var currentUserId = GetCurrentUserId();
            var userToUpdate = await _authService.GetUserByIdAsync(id);
            
            if (userToUpdate == null)
            {
                return NotFound();
            }

            // Check if this is a protected user
            if (IsProtectedUser(userToUpdate, currentUserId))
            {
                // Prevent deactivating protected users
                if (request.IsActive.HasValue && !request.IsActive.Value)
                {
                    var message = userToUpdate.Id == currentUserId 
                        ? "You cannot deactivate your own account"
                        : "Cannot deactivate admin accounts";
                    return BadRequest(new { message });
                }

                // Prevent changing role of protected users
                if (!string.IsNullOrWhiteSpace(request.Role) && request.Role != userToUpdate.Role)
                {
                    var message = userToUpdate.Id == currentUserId 
                        ? "You cannot change your own role"
                        : "Cannot change the role of admin accounts";
                    return BadRequest(new { message });
                }
            }

            var success = await _authService.UpdateUserAsync(id, request);
            
            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var currentUserId = GetCurrentUserId();
            var userToDelete = await _authService.GetUserByIdAsync(id);
            
            if (userToDelete == null)
            {
                return NotFound();
            }

            // Check if this is a protected user
            if (IsProtectedUser(userToDelete, currentUserId))
            {
                var message = userToDelete.Id == currentUserId 
                    ? "You cannot delete your own account"
                    : "Cannot delete admin accounts";
                return BadRequest(new { message });
            }

            var success = await _authService.DeleteUserAsync(id);
            
            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpPut("{id}/clients")]
        public async Task<IActionResult> UpdateUserClients(string id, [FromBody] List<string> clientIds)
        {
            var currentUserId = GetCurrentUserId();
            var userToUpdate = await _authService.GetUserByIdAsync(id);
            
            if (userToUpdate == null)
            {
                return NotFound();
            }

            // Check if this is a protected user and prevent modifying admin client assignments
            if (IsProtectedUser(userToUpdate, currentUserId) && userToUpdate.Id != currentUserId)
            {
                return BadRequest(new { message = "Cannot modify client assignments for admin accounts" });
            }

            var request = new Api.Services.UpdateUserRequest
            {
                AssignedClientIds = clientIds
            };

            var success = await _authService.UpdateUserAsync(id, request);
            
            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpPost("change-password")]
        [AllowAnonymous]
        [Authorize] // Any authenticated user can change their own password
        public async Task<IActionResult> ChangePassword([FromBody] Api.Services.ChangePasswordRequest request)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (userId == null)
            {
                return Unauthorized();
            }

            var success = await _authService.ChangePasswordAsync(userId, request);
            
            if (!success)
            {
                return BadRequest(new { message = "Current password is incorrect" });
            }

            return Ok(new { message = "Password changed successfully" });
        }

        [HttpPost("generate-api-key")]
        [AllowAnonymous]
        [Authorize] // Any authenticated user can generate their own API key
        public async Task<IActionResult> GenerateApiKey()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (userId == null)
            {
                return Unauthorized();
            }

            try
            {
                var apiKey = await _authService.GenerateApiKeyAsync(userId);
                return Ok(new { apiKey });
            }
            catch (ArgumentException)
            {
                return NotFound();
            }
        }


        [HttpPost("{id}/reset-password")]
        public async Task<IActionResult> ResetUserPassword(string id, [FromBody] ResetPasswordRequest request)
        {
            var currentUserId = GetCurrentUserId();
            var userToReset = await _authService.GetUserByIdAsync(id);
            
            if (userToReset == null)
            {
                return NotFound();
            }

            // Check if this is a protected user and if current user is trying to reset another admin's password
            if (IsProtectedUser(userToReset, currentUserId) && userToReset.Id != currentUserId)
            {
                return BadRequest(new { message = "Cannot reset password for admin accounts" });
            }

            var success = await _authService.ResetUserPasswordAsync(id, request.NewPassword);
            
            if (!success)
            {
                return NotFound();
            }

            return Ok(new { message = "Password reset successfully" });
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetUserList(
            [FromQuery] string? search = null,
            [FromQuery] int limit = 1000, // Default high limit for backward compatibility
            [FromQuery] int offset = 0) // Admin-only access due to controller-level [Authorize(Roles = UserRoles.Admin)]
        {
            var users = await _authService.GetUsersAsync();
            // Apply search filter
            var filteredUsers = users
                .Where(u => u.IsActive) // Include all active users (including admins for filtering)
                .Where(u => string.IsNullOrWhiteSpace(search) || 
                           u.Username.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                           (!string.IsNullOrWhiteSpace(u.FirstName) && u.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                           (!string.IsNullOrWhiteSpace(u.LastName) && u.LastName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                           u.Email.Contains(search, StringComparison.OrdinalIgnoreCase))
                .OrderBy(u => !string.IsNullOrWhiteSpace(u.FirstName) && !string.IsNullOrWhiteSpace(u.LastName)
                    ? $"{u.FirstName} {u.LastName}"
                    : u.Username)
                .ToList();
            
            // Calculate pagination
            var totalCount = filteredUsers.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)limit);
            var currentPage = (offset / limit) + 1;
            
            // Apply pagination and map to DTO
            var paginatedUsers = filteredUsers
                .Skip(offset)
                .Take(limit)
                .Select(u => new UserListItemWithClients
                {
                    Id = u.Id,
                    Name = !string.IsNullOrWhiteSpace(u.FirstName) && !string.IsNullOrWhiteSpace(u.LastName)
                        ? $"{u.FirstName} {u.LastName} ({u.Username})"
                        : u.Username,
                    Username = u.Username,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    AssignedClientIds = u.AssignedClientIds ?? new List<string>()
                })
                .ToList();
            
            return Ok(new PaginatedResponse<UserListItemWithClients>
            {
                Data = paginatedUsers,
                CurrentPage = currentPage,
                PageSize = limit,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasPrevious = offset > 0,
                HasNext = (offset + limit) < totalCount
            });
        }

        [HttpGet("{id}/clients")]
        public async Task<IActionResult> GetUserClients(string id)
        {
            var user = await _authService.GetUserByIdAsync(id);
            
            if (user == null)
            {
                return NotFound();
            }

            // If user is admin, return all clients
            if (user.Role == UserRoles.Admin)
            {
                var allClients = await _clientRepository.GetAllAsync();
                var clientDtos = allClients
                    .Select(c => new { c.Id, c.Name })
                    .OrderBy(c => c.Name)
                    .ToList();
                
                return Ok(new { data = clientDtos });
            }

            // For regular users, return only assigned clients
            var assignedClientIds = user.AssignedClientIds ?? new List<string>();
            var assignedClients = new List<object>();

            foreach (var clientId in assignedClientIds)
            {
                var client = await _clientRepository.GetByIdAsync(clientId);
                if (client != null)
                {
                    assignedClients.Add(new { client.Id, client.Name });
                }
            }

            return Ok(new { data = assignedClients.OrderBy(c => ((dynamic)c).Name) });
        }

    }

    public class ResetPasswordRequest
    {
        public string NewPassword { get; set; } = string.Empty;
    }

    public class UserListItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class UserListItemWithClients
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public List<string> AssignedClientIds { get; set; } = new();
    }
}