using LeadHype.Api.Core.Models.Auth;
using Dapper;
using Newtonsoft.Json;
using System.Data;

namespace LeadHype.Api.Core.Database.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDbConnectionService _connectionService;

    public UserRepository(IDbConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        const string sql = @"
            SELECT 
                id,
                email,
                username,
                password_hash,
                role,
                first_name,
                last_name,
                is_active,
                created_at,
                last_login_at,
                refresh_token,
                refresh_token_expiry_time,
                assigned_client_ids::text as assigned_client_ids_json,
                api_key,
                api_key_created_at
            FROM users
            ORDER BY created_at ASC";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync(sql);
        
        return results.Select(MapToUser);
    }

    public async Task<User?> GetByIdAsync(string id)
    {
        const string sql = @"
            SELECT 
                id,
                email,
                username,
                password_hash,
                role,
                first_name,
                last_name,
                is_active,
                created_at,
                last_login_at,
                refresh_token,
                refresh_token_expiry_time,
                assigned_client_ids::text as assigned_client_ids_json,
                api_key,
                api_key_created_at
            FROM users
            WHERE id = @Id";

        using var connection = await _connectionService.GetConnectionAsync();
        var result = await connection.QueryFirstOrDefaultAsync(sql, new { Id = id });
        
        return result != null ? MapToUser(result) : null;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        const string sql = @"
            SELECT 
                id,
                email,
                username,
                password_hash,
                role,
                first_name,
                last_name,
                is_active,
                created_at,
                last_login_at,
                refresh_token,
                refresh_token_expiry_time,
                assigned_client_ids::text as assigned_client_ids_json,
                api_key,
                api_key_created_at
            FROM users
            WHERE email = @Email";

        using var connection = await _connectionService.GetConnectionAsync();
        var result = await connection.QueryFirstOrDefaultAsync(sql, new { Email = email });
        
        return result != null ? MapToUser(result) : null;
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        const string sql = @"
            SELECT 
                id,
                email,
                username,
                password_hash,
                role,
                first_name,
                last_name,
                is_active,
                created_at,
                last_login_at,
                refresh_token,
                refresh_token_expiry_time,
                assigned_client_ids::text as assigned_client_ids_json,
                api_key,
                api_key_created_at
            FROM users
            WHERE username = @Username";

        using var connection = await _connectionService.GetConnectionAsync();
        var result = await connection.QueryFirstOrDefaultAsync(sql, new { Username = username });
        
        return result != null ? MapToUser(result) : null;
    }

    public async Task<string> CreateAsync(User user)
    {
        const string sql = @"
            INSERT INTO users (
                id,
                email,
                username,
                password_hash,
                role,
                first_name,
                last_name,
                is_active,
                created_at,
                last_login_at,
                refresh_token,
                refresh_token_expiry_time,
                assigned_client_ids
            )
            VALUES (
                @Id,
                @Email,
                @Username,
                @PasswordHash,
                @Role,
                @FirstName,
                @LastName,
                @IsActive,
                @CreatedAt,
                @LastLoginAt,
                @RefreshToken,
                @RefreshTokenExpiryTime,
                @AssignedClientIdsJson::jsonb
            )
            RETURNING id";

        user.Id = user.Id ?? Guid.NewGuid().ToString();
        user.CreatedAt = DateTime.UtcNow;

        using var connection = await _connectionService.GetConnectionAsync();
        return await connection.QuerySingleAsync<string>(sql, new
        {
            user.Id,
            user.Email,
            user.Username,
            user.PasswordHash,
            user.Role,
            user.FirstName,
            user.LastName,
            user.IsActive,
            user.CreatedAt,
            user.LastLoginAt,
            user.RefreshToken,
            user.RefreshTokenExpiryTime,
            AssignedClientIdsJson = JsonConvert.SerializeObject(user.AssignedClientIds)
        });
    }

    public async Task<bool> UpdateAsync(User user)
    {
        const string sql = @"
            UPDATE users SET
                email = @Email,
                username = @Username,
                password_hash = @PasswordHash,
                role = @Role,
                first_name = @FirstName,
                last_name = @LastName,
                is_active = @IsActive,
                last_login_at = @LastLoginAt,
                refresh_token = @RefreshToken,
                refresh_token_expiry_time = @RefreshTokenExpiryTime,
                assigned_client_ids = @AssignedClientIdsJson::jsonb,
                api_key = @ApiKey,
                api_key_created_at = @ApiKeyCreatedAt,
                updated_at = @UpdatedAt
            WHERE id = @Id";

        using var connection = await _connectionService.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            user.Id,
            user.Email,
            user.Username,
            user.PasswordHash,
            user.Role,
            user.FirstName,
            user.LastName,
            user.IsActive,
            user.LastLoginAt,
            user.RefreshToken,
            user.RefreshTokenExpiryTime,
            AssignedClientIdsJson = JsonConvert.SerializeObject(user.AssignedClientIds),
            user.ApiKey,
            user.ApiKeyCreatedAt,
            UpdatedAt = DateTime.UtcNow
        });
        
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        const string sql = "DELETE FROM users WHERE id = @Id";

        using var connection = await _connectionService.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }

    public async Task<int> CountAsync()
    {
        const string sql = "SELECT COUNT(*) FROM users";

        using var connection = await _connectionService.GetConnectionAsync();
        return await connection.QuerySingleAsync<int>(sql);
    }

    private static User MapToUser(dynamic result)
    {
        var assignedClientIds = new List<string>();
        if (!string.IsNullOrEmpty(result.assigned_client_ids_json))
        {
            try
            {
                assignedClientIds = JsonConvert.DeserializeObject<List<string>>(result.assigned_client_ids_json) ?? new List<string>();
            }
            catch
            {
                assignedClientIds = new List<string>();
            }
        }

        return new User
        {
            Id = result.id,
            Email = result.email,
            Username = result.username,
            PasswordHash = result.password_hash,
            Role = result.role,
            FirstName = result.first_name,
            LastName = result.last_name,
            IsActive = result.is_active,
            CreatedAt = result.created_at,
            LastLoginAt = result.last_login_at,
            RefreshToken = result.refresh_token,
            RefreshTokenExpiryTime = result.refresh_token_expiry_time,
            AssignedClientIds = assignedClientIds,
            ApiKey = result.api_key,
            ApiKeyCreatedAt = result.api_key_created_at
        };
    }
}