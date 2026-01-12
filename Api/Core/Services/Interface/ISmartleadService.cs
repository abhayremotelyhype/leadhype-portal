namespace LeadHype.Api;

public interface ISmartleadService
{
    bool TryGetAccount(string email, out SmartleadAccount? account);
    Task<bool> AuthenticateAsync(SmartleadAccount account);
    Task<bool> AuthenticateAsync(string username, string password);
    Task<int?> SearchEmailAccountAsync(string email);
    Task<bool> VerifyCNameAsync(int accountId, string cname);
    Task<bool> SaveGeneralAsync(int accountId, GeneralSettings settings);
    Task<bool> ConfigureWarmupAsync(int accountId, WarmupSettings settings);
    Task<List<ClientDto>> GetClientsAsync(int? limit);
    Task<bool> DeleteAccountAsync(long? accountId);

    List<SmartleadAccount> GetAccountsAsync();

    // Task<List<SmartleadAccount>> GetAccountsAsync();
    // Task<SmartleadAccount?> GetAccountByIdAsync(string id);
    // Task<SmartleadAccount?> GetAccountByEmailAsync(string email);
    // Task<CreateAccountResponse> CreateAccountAsync(CreateAccountRequest request);
    // Task<bool> DeleteAccountAsync(string id);
    // Task<bool> RefreshAccountAsync(string id);
    // Task<List<EmailAccount>> GetAccountEmailsAsync(string accountId);
    // Task<EmailAccount> AddEmailAccountAsync(string accountId, EmailAccount emailAccount);
    // Task<bool> UpdateAccountStatsAsync(string accountId);
}