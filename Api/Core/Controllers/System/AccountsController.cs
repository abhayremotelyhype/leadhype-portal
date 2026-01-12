using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LeadHype.Api.Core.Models;
using LeadHype.Api.Core.Models.Api;

namespace LeadHype.Api.Controllers;

[ApiController]
[Route("api/accounts")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(ILogger<AccountsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get all admin accounts
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAccounts()
    {
        try
        {
            // For now, return empty array since the accounts feature is not fully implemented
            var accounts = new List<AdminAccount>();
            
            return Ok(ApiResponse<IEnumerable<AdminAccount>>.SuccessResponse(accounts));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving accounts");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to retrieve accounts", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// Get email accounts for a specific admin account
    /// </summary>
    [HttpGet("{accountId}/email-accounts")]
    public async Task<IActionResult> GetEmailAccounts(string accountId)
    {
        try
        {
            // For now, return empty array since the accounts feature is not fully implemented
            var emailAccounts = new List<EmailAccountInfo>();
            
            return Ok(ApiResponse<IEnumerable<EmailAccountInfo>>.SuccessResponse(emailAccounts));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving email accounts for account {AccountId}", accountId);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to retrieve email accounts", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// Add a new admin account
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddAccount([FromBody] CreateAccountRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Invalid request data", "INVALID_REQUEST"));
            }

            // For now, return a placeholder response since the accounts feature is not fully implemented
            var account = new AdminAccount
            {
                Id = Guid.NewGuid().ToString(),
                ApiKey = request.ApiKey,
                Email = request.Email,
                Status = "Active",
                EmailAccountsCount = 0,
                CampaignsCount = 0,
                TotalSent = 0,
                TotalOpened = 0,
                TotalReplied = 0,
                TotalLeads = 0,
                OpenRate = 0,
                ReplyRate = 0,
                CreatedAt = DateTime.UtcNow
            };
            
            var response = ApiResponse<AdminAccount>.SuccessResponse(account);
            // We can add a message through a custom property or wrapper, but for now just return success response
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding account");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to add account", "INTERNAL_ERROR"));
        }
    }
}

public class AdminAccount
{
    public string Id { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int EmailAccountsCount { get; set; }
    public int CampaignsCount { get; set; }
    public long TotalSent { get; set; }
    public long TotalOpened { get; set; }
    public long TotalReplied { get; set; }
    public long TotalLeads { get; set; }
    public decimal OpenRate { get; set; }
    public decimal ReplyRate { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class EmailAccountInfo
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Sent { get; set; }
    public int Opened { get; set; }
    public int Replied { get; set; }
    public int Bounced { get; set; }
}

public class CreateAccountRequest
{
    public string ApiKey { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}