using LeadHype.Api.Models;
using Dapper;
using System.Data;

namespace LeadHype.Api.Core.Database.Repositories;

public class ClientRepository : IClientRepository
{
    private readonly IDbConnectionService _connectionService;

    public ClientRepository(IDbConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    public async Task<IEnumerable<Client>> GetAllAsync()
    {
        const string sql = @"
            SELECT 
                id,
                name,
                email,
                company,
                status,
                color,
                notes,
                created_at,
                updated_at
            FROM clients
            ORDER BY created_at DESC";

        using var connection = await _connectionService.GetConnectionAsync();
        return await connection.QueryAsync<Client>(sql);
    }

    public async Task<Client?> GetByIdAsync(string id)
    {
        const string sql = @"
            SELECT 
                id,
                name,
                email,
                company,
                status,
                color,
                notes,
                created_at,
                updated_at
            FROM clients
            WHERE id = @Id";

        using var connection = await _connectionService.GetConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<Client>(sql, new { Id = id });
    }

    public async Task<string> CreateAsync(Client client)
    {
        const string sql = @"
            INSERT INTO clients (
                id,
                name,
                email,
                company,
                status,
                color,
                notes,
                created_at,
                updated_at
            )
            VALUES (
                @Id,
                @Name,
                @Email,
                @Company,
                @Status,
                @Color,
                @Notes,
                @CreatedAt,
                @UpdatedAt
            )
            RETURNING id";

        client.Id = client.Id ?? Guid.NewGuid().ToString();
        client.CreatedAt = DateTime.UtcNow;
        client.UpdatedAt = DateTime.UtcNow;

        using var connection = await _connectionService.GetConnectionAsync();
        return await connection.QuerySingleAsync<string>(sql, client);
    }

    public async Task<bool> UpdateAsync(Client client)
    {
        const string sql = @"
            UPDATE clients SET
                name = @Name,
                email = @Email,
                company = @Company,
                status = @Status,
                color = @Color,
                notes = @Notes,
                updated_at = @UpdatedAt
            WHERE id = @Id";

        client.UpdatedAt = DateTime.UtcNow;

        using var connection = await _connectionService.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, client);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        const string sql = "DELETE FROM clients WHERE id = @Id";

        using var connection = await _connectionService.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }

    public async Task<int> CountAsync()
    {
        const string sql = "SELECT COUNT(*) FROM clients";

        using var connection = await _connectionService.GetConnectionAsync();
        return await connection.QuerySingleAsync<int>(sql);
    }
}