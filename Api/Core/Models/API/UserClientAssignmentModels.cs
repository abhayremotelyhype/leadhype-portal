using System.ComponentModel.DataAnnotations;

namespace LeadHype.Api.Core.Models.API;

/// <summary>
/// Request model for assigning clients to a user
/// </summary>
public class AssignClientsToUserRequest
{
    /// <summary>
    /// The ID of the user to assign clients to
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// List of client IDs to assign to the user. Pass empty array to remove all assignments.
    /// </summary>
    [Required]
    public List<string> ClientIds { get; set; } = new();
}

/// <summary>
/// Response model for client assignment operations
/// </summary>
public class AssignClientsToUserResponse
{
    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Optional message providing additional details
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// The updated user information with assigned clients
    /// </summary>
    public UserWithClientsInfo? User { get; set; }
}

/// <summary>
/// User information with assigned client details
/// </summary>
public class UserWithClientsInfo
{
    /// <summary>
    /// User ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// User email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// User's first name
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// User's last name
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// User's role
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Whether the user is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// List of assigned client IDs
    /// </summary>
    public List<string> AssignedClientIds { get; set; } = new();

    /// <summary>
    /// List of assigned client details
    /// </summary>
    public List<AssignedClientInfo> AssignedClients { get; set; } = new();
}

/// <summary>
/// Information about an assigned client
/// </summary>
public class AssignedClientInfo
{
    /// <summary>
    /// Client ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Client name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Client color (for UI display)
    /// </summary>
    public string? Color { get; set; }
}