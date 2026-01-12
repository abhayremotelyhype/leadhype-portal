using LeadHype.Api.Core.Models.Auth;
using Dapper;

namespace LeadHype.Api.Core.Database.Repositories;

public class UserSessionRepository : IUserSessionRepository
{
    private readonly IDbConnectionService _connectionService;

    public UserSessionRepository(IDbConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    public async Task<IEnumerable<UserSession>> GetAllAsync()
    {
        const string sql = @"
            SELECT 
                id,
                user_id,
                refresh_token,
                refresh_token_expiry_time,
                created_at,
                last_accessed_at,
                device_name,
                ip_address,
                user_agent,
                is_active
            FROM user_sessions
            ORDER BY created_at DESC";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync(sql);
        return results.Select(MapToUserSession);
    }

    public async Task<UserSession?> GetByIdAsync(string id)
    {
        const string sql = @"
            SELECT 
                id,
                user_id,
                refresh_token,
                refresh_token_expiry_time,
                created_at,
                last_accessed_at,
                device_name,
                ip_address,
                user_agent,
                is_active
            FROM user_sessions
            WHERE id = @Id";

        using var connection = await _connectionService.GetConnectionAsync();
        var result = await connection.QueryFirstOrDefaultAsync(sql, new { Id = id });
        return result != null ? MapToUserSession(result) : null;
    }

    public async Task<UserSession?> GetByRefreshTokenAsync(string refreshToken)
    {
        const string sql = @"
            SELECT 
                id,
                user_id,
                refresh_token,
                refresh_token_expiry_time,
                created_at,
                last_accessed_at,
                device_name,
                ip_address,
                user_agent,
                is_active
            FROM user_sessions
            WHERE refresh_token = @RefreshToken 
                AND is_active = true 
                AND refresh_token_expiry_time > @Now";

        using var connection = await _connectionService.GetConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<UserSession>(sql, new { 
            RefreshToken = refreshToken,
            Now = DateTime.UtcNow 
        });
    }

    public async Task<IEnumerable<UserSession>> GetByUserIdAsync(string userId)
    {
        const string sql = @"
            SELECT 
                id,
                user_id,
                refresh_token,
                refresh_token_expiry_time,
                created_at,
                last_accessed_at,
                device_name,
                ip_address,
                user_agent,
                is_active
            FROM user_sessions
            WHERE user_id = @UserId
            ORDER BY created_at DESC";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync(sql, new { UserId = userId });
        return results.Select(MapToUserSession);
    }

    public async Task<IEnumerable<UserSession>> GetActiveSessionsByUserIdAsync(string userId)
    {
        const string sql = @"
            SELECT
                id,
                user_id,
                refresh_token,
                refresh_token_expiry_time,
                created_at,
                last_accessed_at,
                device_name,
                ip_address,
                user_agent,
                is_active
            FROM user_sessions
            WHERE user_id = @UserId
                AND is_active = true
                AND refresh_token_expiry_time > @Now
                AND created_at > @OldSessionThreshold
            ORDER BY last_accessed_at DESC";

        using var connection = await _connectionService.GetConnectionAsync();
        return await connection.QueryAsync<UserSession>(sql, new {
            UserId = userId,
            Now = DateTime.UtcNow,
            OldSessionThreshold = DateTime.UtcNow.AddDays(-30)
        });
    }

    public async Task<IEnumerable<UserSession>> GetExpiredSessionsAsync()
    {
        const string sql = @"
            SELECT
                id,
                user_id,
                refresh_token,
                refresh_token_expiry_time,
                created_at,
                last_accessed_at,
                device_name,
                ip_address,
                user_agent,
                is_active
            FROM user_sessions
            WHERE refresh_token_expiry_time <= @Now
                OR is_active = false
                OR created_at <= @OldSessionThreshold";

        using var connection = await _connectionService.GetConnectionAsync();
        return await connection.QueryAsync<UserSession>(sql, new {
            Now = DateTime.UtcNow,
            OldSessionThreshold = DateTime.UtcNow.AddDays(-30)
        });
    }

    public async Task<string> CreateAsync(UserSession session)
    {
        const string sql = @"
            INSERT INTO user_sessions (
                id,
                user_id,
                refresh_token,
                refresh_token_expiry_time,
                created_at,
                last_accessed_at,
                device_name,
                ip_address,
                user_agent,
                is_active
            )
            VALUES (
                @Id,
                @UserId,
                @RefreshToken,
                @RefreshTokenExpiryTime,
                @CreatedAt,
                @LastAccessedAt,
                @DeviceName,
                @IpAddress,
                @UserAgent,
                @IsActive
            )
            RETURNING id";

        session.Id = session.Id ?? Guid.NewGuid().ToString();
        session.CreatedAt = DateTime.UtcNow;
        session.LastAccessedAt = DateTime.UtcNow;

        using var connection = await _connectionService.GetConnectionAsync();
        return await connection.QuerySingleAsync<string>(sql, session);
    }

    public async Task<bool> UpdateAsync(UserSession session)
    {
        const string sql = @"
            UPDATE user_sessions SET
                user_id = @UserId,
                refresh_token = @RefreshToken,
                refresh_token_expiry_time = @RefreshTokenExpiryTime,
                last_accessed_at = @LastAccessedAt,
                device_name = @DeviceName,
                ip_address = @IpAddress,
                user_agent = @UserAgent,
                is_active = @IsActive
            WHERE id = @Id";

        using var connection = await _connectionService.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, session);
        return rowsAffected > 0;
    }

    public async Task<bool> UpdateLastAccessedAsync(string id)
    {
        const string sql = @"
            UPDATE user_sessions SET
                last_accessed_at = @LastAccessedAt
            WHERE id = @Id";

        using var connection = await _connectionService.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, new { 
            Id = id,
            LastAccessedAt = DateTime.UtcNow 
        });
        return rowsAffected > 0;
    }

    public async Task<bool> DeactivateAsync(string id)
    {
        const string sql = @"
            UPDATE user_sessions SET
                is_active = false,
                last_accessed_at = @LastAccessedAt
            WHERE id = @Id";

        using var connection = await _connectionService.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, new { 
            Id = id,
            LastAccessedAt = DateTime.UtcNow 
        });
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        const string sql = "DELETE FROM user_sessions WHERE id = @Id";

        using var connection = await _connectionService.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteByRefreshTokenAsync(string refreshToken)
    {
        const string sql = "DELETE FROM user_sessions WHERE refresh_token = @RefreshToken";

        using var connection = await _connectionService.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, new { RefreshToken = refreshToken });
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteExpiredSessionsAsync()
    {
        const string sql = @"
            DELETE FROM user_sessions
            WHERE refresh_token_expiry_time <= @Now
                OR is_active = false
                OR created_at <= @OldSessionThreshold";

        using var connection = await _connectionService.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, new {
            Now = DateTime.UtcNow,
            OldSessionThreshold = DateTime.UtcNow.AddDays(-30)
        });
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAllUserSessionsAsync(string userId)
    {
        const string sql = "DELETE FROM user_sessions WHERE user_id = @UserId";

        using var connection = await _connectionService.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, new { UserId = userId });
        return rowsAffected > 0;
    }

    private static UserSession MapToUserSession(dynamic result)
    {
        return new UserSession
        {
            Id = result.id,
            UserId = result.user_id,
            RefreshToken = result.refresh_token,
            RefreshTokenExpiryTime = result.refresh_token_expiry_time,
            CreatedAt = result.created_at,
            LastAccessedAt = result.last_accessed_at,
            DeviceName = result.device_name,
            IpAddress = result.ip_address,
            UserAgent = result.user_agent,
            IsActive = result.is_active
        };
    }
}