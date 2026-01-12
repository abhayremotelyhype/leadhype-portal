using LeadHype.Api.Core.Models.Auth;
using LeadHype.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LeadHype.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IRateLimitService _rateLimitService;

        public AuthController(IAuthService authService, IRateLimitService rateLimitService)
        {
            _authService = authService;
            _rateLimitService = rateLimitService;
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
            return user.Role == UserRoles.Admin;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Get client identifier (IP + email combination for more granular control)
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var rateLimitKey = $"login:{clientIp}:{request.Email.ToLower()}";
            
            // Check rate limit (5 attempts per 15 minutes)
            var isAllowed = await _rateLimitService.IsAllowedAsync(rateLimitKey, 5, TimeSpan.FromMinutes(15));
            if (!isAllowed)
            {
                return StatusCode((int)HttpStatusCode.TooManyRequests, 
                    new { message = "Too many login attempts. Please try again in 15 minutes." });
            }

            // Get IP address and user agent for session tracking
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
            
            var result = await _authService.AuthenticateAsync(request, ipAddress, userAgent);
            
            if (!result.Success)
            {
                return Unauthorized(new { message = result.ErrorMessage });
            }

            // Reset rate limit on successful login
            await _rateLimitService.ResetAsync(rateLimitKey);
            
            return Ok(result.LoginResponse);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var response = await _authService.RefreshTokenAsync(request.RefreshToken);
            
            if (response == null)
            {
                return Unauthorized(new { message = "Invalid refresh token" });
            }

            return Ok(response);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            // Get the refresh token from the request
            var authHeader = Request.Headers["Authorization"].ToString();
            var refreshToken = Request.Headers["X-Refresh-Token"].ToString();
            
            if (string.IsNullOrEmpty(refreshToken))
            {
                return BadRequest(new { message = "Refresh token required" });
            }

            await _authService.RevokeTokenAsync(refreshToken);
            return Ok(new { message = "Logged out successfully" });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest();
            }

            var user = await _authService.GetUserByIdAsync(userId);
            
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
                AssignedClientIds = user.AssignedClientIds ?? new List<string>(),
                ApiKey = user.ApiKey,
                ApiKeyCreatedAt = user.ApiKeyCreatedAt
            });
        }

        [HttpGet("sessions")]
        [Authorize]
        public async Task<IActionResult> GetSessions()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest();
            }

            var sessions = await _authService.GetUserSessionsAsync(userId);
            
            // Get current refresh token securely without exposing it in comparison
            var currentRefreshToken = Request.Headers["X-Refresh-Token"].FirstOrDefault();
            
            return Ok(sessions.Select(s => new
            {
                s.Id,
                s.DeviceName,
                s.IpAddress,
                s.CreatedAt,
                s.LastAccessedAt,
                IsCurrent = !string.IsNullOrEmpty(currentRefreshToken) && 
                           !string.IsNullOrEmpty(s.RefreshToken) && 
                           currentRefreshToken.Equals(s.RefreshToken, StringComparison.Ordinal)
            }));
        }

        [HttpDelete("sessions/{sessionId}")]
        [Authorize]
        public async Task<IActionResult> RevokeSession(string sessionId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest();
            }

            // Verify the session belongs to the user
            var sessions = await _authService.GetUserSessionsAsync(userId);
            var session = sessions.FirstOrDefault(s => s.Id == sessionId);
            
            if (session == null)
            {
                return NotFound(new { message = "Session not found" });
            }

            await _authService.RevokeSessionAsync(sessionId);
            return Ok(new { message = "Session revoked successfully" });
        }

        [HttpPost("logout-all")]
        [Authorize]
        public async Task<IActionResult> LogoutAll()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest();
            }

            await _authService.RevokeAllUserSessionsAsync(userId);
            return Ok(new { message = "All sessions logged out successfully" });
        }

        [HttpGet("user-sessions/{userId}")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> GetUserSessions(string userId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var user = await _authService.GetUserByIdAsync(userId);
                
                if (user == null)
                {
                    return NotFound(new { message = $"User with id {userId} not found" });
                }

                // Check if this is a protected user and if current user is trying to view another admin's sessions
                if (IsProtectedUser(user, currentUserId) && user.Id != currentUserId)
                {
                    return BadRequest(new { message = "Cannot view sessions for admin accounts" });
                }

                var sessions = await _authService.GetUserSessionsAsync(userId);
                
                var sessionDtos = sessions.Select(s => new
                {
                    s.Id,
                    s.DeviceName,
                    s.IpAddress,
                    s.CreatedAt,
                    s.LastAccessedAt,
                    IsCurrent = false
                }).ToList();
                
                return Ok(sessionDtos);
            }
            catch (Exception ex)
            {
                // Log the full exception but don't expose details to client
                // Consider using ILogger instead of exposing exception details
                return StatusCode(500, new { message = "An error occurred while fetching user sessions" });
            }
        }

        [HttpDelete("admin/users/{userId}/sessions/{sessionId}")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> RevokeUserSession(string userId, string sessionId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var targetUser = await _authService.GetUserByIdAsync(userId);
                
                if (targetUser == null)
                {
                    return NotFound(new { message = $"User with id {userId} not found" });
                }

                // Check if this is a protected user and if current user is trying to revoke another admin's session
                if (IsProtectedUser(targetUser, currentUserId) && targetUser.Id != currentUserId)
                {
                    return BadRequest(new { message = "Cannot revoke sessions for admin accounts" });
                }

                // Verify the session belongs to the target user
                var sessions = await _authService.GetUserSessionsAsync(userId);
                var session = sessions.FirstOrDefault(s => s.Id == sessionId);
                
                if (session == null)
                {
                    return NotFound(new { message = "Session not found for this user" });
                }

                await _authService.RevokeSessionAsync(sessionId);
                return Ok(new { message = "User session revoked successfully" });
            }
            catch (Exception ex)
            {
                // Log the full exception but don't expose details to client
                return StatusCode(500, new { message = "An error occurred while revoking user session" });
            }
        }

        [HttpDelete("admin/users/{userId}/sessions")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> RevokeAllUserSessions(string userId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var targetUser = await _authService.GetUserByIdAsync(userId);
                
                if (targetUser == null)
                {
                    return NotFound(new { message = $"User with id {userId} not found" });
                }

                // Check if this is a protected user and if current user is trying to revoke another admin's sessions
                if (IsProtectedUser(targetUser, currentUserId) && targetUser.Id != currentUserId)
                {
                    return BadRequest(new { message = "Cannot revoke sessions for admin accounts" });
                }

                await _authService.RevokeAllUserSessionsAsync(userId);
                return Ok(new { message = "All user sessions revoked successfully" });
            }
            catch (Exception ex)
            {
                // Log the full exception but don't expose details to client
                return StatusCode(500, new { message = "An error occurred while revoking user sessions" });
            }
        }
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; }
    }
}