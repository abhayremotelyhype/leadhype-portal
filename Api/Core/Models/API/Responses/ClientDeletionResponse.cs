using System.ComponentModel.DataAnnotations;

namespace LeadHype.Api.Core.Models.API.Responses
{
    /// <summary>
    /// Response model for client deletion
    /// </summary>
    public class ClientDeletionResponse
    {
        /// <summary>
        /// Indicates if the client was deleted successfully
        /// </summary>
        /// <example>true</example>
        public bool Success { get; set; }
        
        /// <summary>
        /// Response message
        /// </summary>
        /// <example>Client deleted successfully</example>
        public string Message { get; set; } = string.Empty;
    }
}