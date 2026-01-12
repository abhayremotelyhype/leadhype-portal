using LeadHype.Api.Core.Database.Repositories;
using LeadHype.Api.Core.Models.Auth;

namespace LeadHype.Api.Services
{
    public interface IDatabaseInitializationService
    {
        Task InitializeAsync();
    }

    public class DatabaseInitializationService : IDatabaseInitializationService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<DatabaseInitializationService> _logger;

        public DatabaseInitializationService(IUserRepository userRepository, ILogger<DatabaseInitializationService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            await CreateDefaultAdminUser();
        }

        private async Task CreateDefaultAdminUser()
        {
            try
            {
                // Check if any admin user exists by checking if admin@leadhype.com exists
                var existingAdmin = await _userRepository.GetByEmailAsync("admin@leadhype.com");
                
                if (existingAdmin == null)
                {
                    // Create default admin user
                    var defaultAdmin = new User
                    {
                        Email = "admin@leadhype.com",
                        Username = "admin",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                        Role = UserRoles.Admin,
                        FirstName = "System",
                        LastName = "Administrator",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _userRepository.CreateAsync(defaultAdmin);
                    _logger.LogInformation("Default admin user created: admin@leadhype.com / Admin123!");
                }
                else
                {
                    _logger.LogInformation("Default admin user already exists");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create default admin user");
                throw;
            }
        }
    }
}