using System.ComponentModel.DataAnnotations;

namespace LeadHype.Api.Core.Models.API.Responses
{
    /// <summary>
    /// Response model for client update
    /// </summary>
    public class ClientUpdateResponse
    {
        /// <summary>
        /// Indicates if the client was updated successfully
        /// </summary>
        /// <example>true</example>
        public bool Success { get; set; }
        
        /// <summary>
        /// The updated client data
        /// </summary>
        public ClientListItem Data { get; set; } = new();
        
        /// <summary>
        /// Success message
        /// </summary>
        /// <example>Client updated successfully</example>
        public string Message { get; set; } = string.Empty;
    }
}