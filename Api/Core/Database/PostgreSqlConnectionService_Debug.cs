using System.Data;
using Npgsql;
using Microsoft.Extensions.Logging;

namespace LeadHype.Api.Core.Database;

/// <summary>
/// Debug version of PostgreSQL connection service to help diagnose connection string issues
/// </summary>
public class PostgreSqlConnectionService_Debug : IDbConnectionService
{
    private readonly string _connectionString;
    private readonly ILogger<PostgreSqlConnectionService_Debug> _logger;
    
    public PostgreSqlConnectionService_Debug(IConfiguration configuration, ILogger<PostgreSqlConnectionService_Debug> logger)
    {
        _logger = logger;
        
        _logger.LogInformation("=== DEBUGGING PostgreSQL Connection Service ===");
        
        // Debug: Check if configuration is null
        if (configuration == null)
        {
            _logger.LogError("IConfiguration is NULL!");
            throw new ArgumentNullException(nameof(configuration), "IConfiguration cannot be null");
        }
        
        // Debug: Log all connection strings
        var connectionStrings = configuration.GetSection("ConnectionStrings");
        _logger.LogInformation("Available connection string keys:");
        foreach (var child in connectionStrings.GetChildren())
        {
            var value = child.Value;
            var displayValue = string.IsNullOrEmpty(value) ? "(empty)" : value.Substring(0, Math.Min(50, value.Length)) + "...";
            _logger.LogInformation("- {Key}: {Value}", child.Key, displayValue);
        }
        
        // Try multiple ways to get the connection string
        var connectionString1 = configuration.GetConnectionString("DefaultConnection");
        var connectionString2 = configuration["ConnectionStrings:DefaultConnection"];
        var connectionString3 = configuration.GetValue<string>("ConnectionStrings:DefaultConnection");
        
        _logger.LogInformation("GetConnectionString result: {Result}", connectionString1 ?? "NULL");
        _logger.LogInformation("Direct indexer result: {Result}", connectionString2 ?? "NULL");
        _logger.LogInformation("GetValue result: {Result}", connectionString3 ?? "NULL");
        
        _connectionString = connectionString1 ?? connectionString2 ?? connectionString3;
        
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            _logger.LogError("All connection string retrieval methods returned null or empty!");
            throw new InvalidOperationException("PostgreSQL connection string 'DefaultConnection' is null or empty. Check your appsettings.json configuration.");
        }
        
        _logger.LogInformation("Successfully loaded connection string (length: {Length})", _connectionString.Length);
    }

    public async Task<IDbConnection> CreateConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            _logger.LogError("Connection string is null during connection attempt");
            throw new InvalidOperationException("Connection string is null or empty");
        }
        
        try
        {
            _logger.LogInformation("Creating connection with string: {ConnectionString}", 
                _connectionString.Substring(0, Math.Min(50, _connectionString.Length)) + "...");
            
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            _logger.LogInformation("Connection opened successfully");
            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open connection: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<IDbConnection> GetConnectionAsync()
    {
        return await CreateConnectionAsync();
    }

    public async Task ExecuteInTransactionAsync(Func<IDbConnection, IDbTransaction, Task> action)
    {
        using var connection = await CreateConnectionAsync();
        using var transaction = connection.BeginTransaction();
        
        try
        {
            await action(connection, transaction);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> action)
    {
        using var connection = await CreateConnectionAsync();
        using var transaction = connection.BeginTransaction();
        
        try
        {
            var result = await action(connection, transaction);
            transaction.Commit();
            return result;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}