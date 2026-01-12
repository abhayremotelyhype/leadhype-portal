
namespace LeadHype.Api.Core.Models.Auth;

public class AuthResult
{
    public bool Success { get; set; }
    public AuthErrorType? ErrorType { get; set; }
    public string? ErrorMessage { get; set; }
    public LoginResponse? LoginResponse { get; set; }
}

public enum AuthErrorType
{
    InvalidCredentials,
    AccountInactive,
    UserNotFound,
    InvalidToken,
    TokenExpired,
    GeneralError
}