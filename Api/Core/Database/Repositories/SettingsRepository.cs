using LeadHype.Api.Models;
using Dapper;

namespace LeadHype.Api.Core.Database.Repositories;

public class SettingsRepository : ISettingsRepository
{
    private readonly IDbConnectionService _connectionService;

    public SettingsRepository(IDbConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    public async Task<IEnumerable<Setting>> GetAllAsync()
    {
        const string sql = @"
            SELECT 
                id,
                key,
                value,
                created_at,
                updated_at
            FROM settings
            ORDER BY key";

        using var connection = await _connectionService.GetConnectionAsync();
        return await connection.QueryAsync<Setting>(sql);
    }

    public async Task<Setting?> GetByIdAsync(string id)
    {
        const string sql = @"
            SELECT 
                id,
                key,
                value,
                created_at,
                updated_at
            FROM settings
            WHERE id = @Id";

        using var connection = await _connectionService.GetConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<Setting>(sql, new { Id = id });
    }

    public async Task<Setting?> GetByKeyAsync(string key)
    {
        const string sql = @"
            SELECT 
                id,
                key,
                value,
                created_at,
                updated_at
            FROM settings
            WHERE key = @Key";

        using var connection = await _connectionService.GetConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<Setting>(sql, new { Key = key });
    }

    public async Task<string> CreateAsync(Setting setting)
    {
        const string sql = @"
            INSERT INTO settings (
                id,
                key,
                value,
                created_at,
                updated_at
            )
            VALUES (
                @Id,
                @Key,
                @Value,
                @CreatedAt,
                @UpdatedAt
            )
            ON CONFLICT (key) DO UPDATE SET
                value = @Value,
                updated_at = @UpdatedAt
            RETURNING id";

        setting.Id = setting.Id ?? Guid.NewGuid().ToString();
        setting.CreatedAt = DateTime.UtcNow;
        setting.UpdatedAt = DateTime.UtcNow;

        using var connection = await _connectionService.GetConnectionAsync();
        return await connection.QuerySingleAsync<string>(sql, setting);
    }

    public async Task<bool> UpdateAsync(Setting setting)
    {
        const string sql = @"
            UPDATE settings SET
                key = @Key,
                value = @Value,
                updated_at = @UpdatedAt
            WHERE id = @Id";

        setting.UpdatedAt = DateTime.UtcNow;

        using var connection = await _connectionService.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, setting);
        return rowsAffected > 0;
    }

    public async Task<bool> UpdateByKeyAsync(string key, string value)
    {
        const string sql = @"
            UPDATE settings SET
                value = @Value,
                updated_at = @UpdatedAt
            WHERE key = @Key";

        using var connection = await _connectionService.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, new 
        { 
            Key = key, 
            Value = value, 
            UpdatedAt = DateTime.UtcNow 
        });
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        const string sql = "DELETE FROM settings WHERE id = @Id";

        using var connection = await _connectionService.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteByKeyAsync(string key)
    {
        const string sql = "DELETE FROM settings WHERE key = @Key";

        using var connection = await _connectionService.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, new { Key = key });
        return rowsAffected > 0;
    }
}