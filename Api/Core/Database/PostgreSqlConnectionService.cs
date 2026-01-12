using System.Data;
using Npgsql;
using Microsoft.Extensions.Logging;

namespace LeadHype.Api.Core.Database;

public class PostgreSqlConnectionService : IDbConnectionService, IDisposable
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<PostgreSqlConnectionService> _logger;
    
    public PostgreSqlConnectionService(IConfiguration configuration, ILogger<PostgreSqlConnectionService> logger)
    {
        _logger = logger;
        
        _logger.LogInformation("Initializing PostgreSqlConnectionService with connection pooling...");
        
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogError("PostgreSQL connection string 'DefaultConnection' is null or empty.");
            throw new InvalidOperationException("PostgreSQL connection string 'DefaultConnection' is null or empty. Check your appsettings.json configuration.");
        }
        
        // Log connection info without exposing credentials
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var safeConnectionInfo = $"Host={builder.Host};Database={builder.Database};Port={builder.Port}";
        _logger.LogInformation("PostgreSQL connection configured: {ConnectionInfo}", safeConnectionInfo);
        
        // Create data source with connection pooling
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        _dataSource = dataSourceBuilder.Build();
        _logger.LogInformation("PostgreSQL DataSource created with connection pooling enabled");
    }

    public async Task<IDbConnection> CreateConnectionAsync()
    {
        const int maxRetries = 3;
        const int delayMs = 100;
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogDebug("Getting PostgreSQL connection from pool (attempt {Attempt}/{MaxRetries})...", attempt, maxRetries);
                var connection = await _dataSource.OpenConnectionAsync();
                _logger.LogDebug("PostgreSQL connection obtained from pool successfully");
                return connection;
            }
            catch (Exception ex) when (attempt < maxRetries && (
                ex.Message.Contains("remaining connection slots are reserved") ||
                ex.Message.Contains("connection pool") ||
                ex.Message.Contains("timeout")))
            {
                _logger.LogWarning(ex, "Connection attempt {Attempt} failed, retrying in {DelayMs}ms: {ErrorMessage}", 
                    attempt, delayMs * attempt, ex.Message);
                await Task.Delay(delayMs * attempt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get PostgreSQL connection from pool (attempt {Attempt}): {ErrorMessage}", 
                    attempt, ex.Message);
                throw new InvalidOperationException($"Failed to create PostgreSQL connection: {ex.Message}", ex);
            }
        }
        
        throw new InvalidOperationException($"Failed to create PostgreSQL connection after {maxRetries} attempts");
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

    public void Dispose()
    {
        _dataSource?.Dispose();
        _logger.LogInformation("PostgreSQL DataSource disposed");
    }
}