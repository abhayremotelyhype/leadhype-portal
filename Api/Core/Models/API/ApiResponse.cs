using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace LeadHype.Api.Core.Models.Api
{
    /// <summary>
    /// Standard API response wrapper
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public ApiError? Error { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public static ApiResponse<T> SuccessResponse(T data)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data
            };
        }

        public static ApiResponse<T> ErrorResponse(string message, string? code = null, List<ApiErrorDetail>? details = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Error = new ApiError
                {
                    Message = message,
                    Code = code,
                    Details = details
                }
            };
        }
    }


    /// <summary>
    /// API error information
    /// </summary>
    public class ApiError
    {
        public string Message { get; set; } = string.Empty;
        public string? Code { get; set; }
        public List<ApiErrorDetail>? Details { get; set; }
    }

    /// <summary>
    /// Detailed error information
    /// </summary>
    public class ApiErrorDetail
    {
        public string Field { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Code { get; set; }
    }



    /// <summary>
    /// Common query parameters for API endpoints
    /// </summary>
    public class ApiQueryParams
    {
        [FromQuery(Name = "page")]
        public int Page { get; set; } = 1;
        
        [FromQuery(Name = "pageSize")]
        public int PageSize { get; set; } = 20;
        
        [FromQuery(Name = "sortBy")]
        public string? SortBy { get; set; }
        
        [FromQuery(Name = "sortDirection")]
        public string? SortDirection { get; set; } = "asc";
        
        [FromQuery(Name = "search")]
        public string? Search { get; set; }
        
        [FromQuery(Name = "fields")]
        public string? Fields { get; set; } // Comma-separated field names for field selection
        
        [FromQuery(Name = "dateFrom")]
        public DateTime? DateFrom { get; set; }
        
        [FromQuery(Name = "dateTo")]
        public DateTime? DateTo { get; set; }
        
        public int GetOffset() => (Page - 1) * PageSize;
        public bool IsDescending() => SortDirection?.ToLower() == "desc";
    }

    /// <summary>
    /// Campaign-specific query parameters (simplified for better user experience)
    /// </summary>
    public class CampaignQueryParams : ApiQueryParams
    {
        /// <summary>
        /// Filter by campaign status (e.g., 'active', 'paused', 'completed')
        /// </summary>
        [FromQuery(Name = "status")]
        public string? Status { get; set; }
        
        /// <summary>
        /// Filter by client ID (for users assigned to specific clients)
        /// </summary>
        [FromQuery(Name = "clientId")]
        public string? ClientId { get; set; }
        
        /// <summary>
        /// Filter by multiple client IDs (comma-separated)
        /// </summary>
        [FromQuery(Name = "filterByClientIds")]
        public string? FilterByClientIds { get; set; }
    }
}