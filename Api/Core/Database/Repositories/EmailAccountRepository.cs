using LeadHype.Api.Core.Database.Models;
using Dapper;
using Newtonsoft.Json;
using System.Data;

namespace LeadHype.Api.Core.Database.Repositories;

public class EmailAccountRepository : IEmailAccountRepository
{
    private readonly IDbConnectionService _connectionService;

    public EmailAccountRepository(IDbConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    public async Task<IEnumerable<EmailAccountDbModel>> GetAllAsync()
    {
        const string sql = @"
            SELECT 
                id,
                admin_uuid,
                email,
                name,
                status,
                client_id,
                client_name,
                client_color,
                warmup_sent,
                warmup_replied,
                warmup_saved_from_spam,
                sent,
                opened,
                replied,
                bounced,
                tags::text as tags_json,
                campaign_count,
                active_campaign_count,
                is_sending_actual_emails,
                created_at,
                updated_at,
                last_updated_at,
                warmup_update_datetime,
                notes
            FROM email_accounts
            ORDER BY created_at DESC";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync(sql);
        
        return results.Select(MapToEmailAccount);
    }

    public async Task<EmailAccountDbModel?> GetByIdAsync(long id)
    {
        const string sql = @"
            SELECT 
                id,
                admin_uuid,
                email,
                name,
                status,
                client_id,
                client_name,
                client_color,
                warmup_sent,
                warmup_replied,
                warmup_saved_from_spam,
                sent,
                opened,
                replied,
                bounced,
                tags::text as tags_json,
                campaign_count,
                active_campaign_count,
                is_sending_actual_emails,
                created_at,
                updated_at,
                last_updated_at,
                warmup_update_datetime,
                notes
            FROM email_accounts
            WHERE id = @Id";

        using var connection = await _connectionService.GetConnectionAsync();
        var result = await connection.QueryFirstOrDefaultAsync(sql, new { Id = id });
        
        return result != null ? MapToEmailAccount(result) : null;
    }

    public async Task<EmailAccountDbModel?> GetByEmailAsync(string email)
    {
        const string sql = @"
            SELECT 
                id,
                admin_uuid,
                email,
                name,
                status,
                client_id,
                client_name,
                client_color,
                warmup_sent,
                warmup_replied,
                warmup_saved_from_spam,
                sent,
                opened,
                replied,
                bounced,
                tags::text as tags_json,
                campaign_count,
                active_campaign_count,
                is_sending_actual_emails,
                created_at,
                updated_at,
                last_updated_at,
                warmup_update_datetime,
                notes
            FROM email_accounts
            WHERE email = @Email";

        using var connection = await _connectionService.GetConnectionAsync();
        var result = await connection.QueryFirstOrDefaultAsync(sql, new { Email = email });
        
        return result != null ? MapToEmailAccount(result) : null;
    }

    public async Task<IEnumerable<EmailAccountDbModel>> GetByAdminUuidAsync(string adminUuid)
    {
        const string sql = @"
            SELECT 
                id,
                admin_uuid,
                email,
                name,
                status,
                client_id,
                client_name,
                client_color,
                warmup_sent,
                warmup_replied,
                warmup_saved_from_spam,
                sent,
                opened,
                replied,
                bounced,
                tags::text as tags_json,
                campaign_count,
                active_campaign_count,
                is_sending_actual_emails,
                created_at,
                updated_at,
                last_updated_at,
                warmup_update_datetime,
                notes
            FROM email_accounts
            WHERE admin_uuid = @AdminUuid
            ORDER BY created_at DESC";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync(sql, new { AdminUuid = adminUuid });
        
        return results.Select(MapToEmailAccount);
    }

    public async Task<IEnumerable<EmailAccountDbModel>> GetByClientIdAsync(string clientId)
    {
        const string sql = @"
            SELECT 
                id,
                admin_uuid,
                email,
                name,
                status,
                client_id,
                client_name,
                client_color,
                warmup_sent,
                warmup_replied,
                warmup_saved_from_spam,
                sent,
                opened,
                replied,
                bounced,
                tags::text as tags_json,
                campaign_count,
                created_at,
                updated_at,
                last_updated_at
            FROM email_accounts
            WHERE client_id = @ClientId
            ORDER BY created_at DESC";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync(sql, new { ClientId = clientId });
        
        return results.Select(MapToEmailAccount);
    }

    public async Task<long> CreateAsync(EmailAccountDbModel emailAccount)
    {
        const string sql = @"
            INSERT INTO email_accounts (
                id,
                admin_uuid,
                email,
                name,
                status,
                client_id,
                client_name,
                client_color,
                warmup_sent,
                warmup_replied,
                warmup_saved_from_spam,
                sent,
                opened,
                replied,
                bounced,
                tags,
                campaign_count,
                created_at,
                updated_at,
                last_updated_at,
                warmup_update_datetime
            )
            VALUES (
                @Id,
                @AdminUuid,
                @Email,
                @Name,
                @Status,
                @ClientId,
                @ClientName,
                @ClientColor,
                @WarmupSent,
                @WarmupReplied,
                @WarmupSavedFromSpam,
                @Sent,
                @Opened,
                @Replied,
                @Bounced,
                @TagsJson::jsonb,
                @CampaignCount,
                @CreatedAt,
                @UpdatedAt,
                @LastUpdatedAt,
                @WarmupUpdateDateTime
            )
            RETURNING id";

        emailAccount.CreatedAt = DateTime.UtcNow;
        emailAccount.UpdatedAt = DateTime.UtcNow;

        using var connection = await _connectionService.GetConnectionAsync();
        return await connection.QuerySingleAsync<long>(sql, new
        {
            emailAccount.Id,
            emailAccount.AdminUuid,
            emailAccount.Email,
            emailAccount.Name,
            emailAccount.Status,
            emailAccount.ClientId,
            emailAccount.ClientName,
            emailAccount.ClientColor,
            emailAccount.WarmupSent,
            emailAccount.WarmupReplied,
            emailAccount.WarmupSavedFromSpam,
            emailAccount.Sent,
            emailAccount.Opened,
            emailAccount.Replied,
            emailAccount.Bounced,
            TagsJson = JsonConvert.SerializeObject(emailAccount.Tags),
            emailAccount.CampaignCount,
            emailAccount.CreatedAt,
            emailAccount.UpdatedAt,
            emailAccount.LastUpdatedAt,
            emailAccount.WarmupUpdateDateTime
        });
    }

    public async Task<bool> UpdateAsync(EmailAccountDbModel emailAccount)
    {
        const string sql = @"
            UPDATE email_accounts SET
                admin_uuid = @AdminUuid,
                email = @Email,
                name = @Name,
                status = @Status,
                client_id = @ClientId,
                client_name = @ClientName,
                client_color = @ClientColor,
                warmup_sent = @WarmupSent,
                warmup_replied = @WarmupReplied,
                warmup_saved_from_spam = @WarmupSavedFromSpam,
                sent = @Sent,
                opened = @Opened,
                replied = @Replied,
                bounced = @Bounced,
                tags = @TagsJson::jsonb,
                campaign_count = @CampaignCount,
                updated_at = @UpdatedAt,
                last_updated_at = @LastUpdatedAt,
                warmup_update_datetime = @WarmupUpdateDateTime,
                notes = @Notes
            WHERE id = @Id";

        emailAccount.UpdatedAt = DateTime.UtcNow;

        using var connection = await _connectionService.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            emailAccount.Id,
            emailAccount.AdminUuid,
            emailAccount.Email,
            emailAccount.Name,
            emailAccount.Status,
            emailAccount.ClientId,
            emailAccount.ClientName,
            emailAccount.ClientColor,
            emailAccount.WarmupSent,
            emailAccount.WarmupReplied,
            emailAccount.WarmupSavedFromSpam,
            emailAccount.Sent,
            emailAccount.Opened,
            emailAccount.Replied,
            emailAccount.Bounced,
            TagsJson = JsonConvert.SerializeObject(emailAccount.Tags),
            emailAccount.CampaignCount,
            emailAccount.UpdatedAt,
            emailAccount.LastUpdatedAt,
            emailAccount.WarmupUpdateDateTime,
            emailAccount.Notes
        });
        
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        const string sql = "DELETE FROM email_accounts WHERE id = @Id";

        using var connection = await _connectionService.GetConnectionAsync();
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }

    public async Task<int> CountAsync()
    {
        const string sql = "SELECT COUNT(*) FROM email_accounts";

        using var connection = await _connectionService.GetConnectionAsync();
        return await connection.QuerySingleAsync<int>(sql);
    }

    public async Task<int> CountByStatusAsync(string status)
    {
        const string sql = @"
            SELECT COUNT(*) 
            FROM email_accounts 
            WHERE status = @Status";

        using var connection = await _connectionService.GetConnectionAsync();
        return await connection.QuerySingleAsync<int>(sql, new { Status = status });
    }

    private static EmailAccountDbModel MapToEmailAccount(dynamic result)
    {
        var tags = new List<string>();
        if (!string.IsNullOrEmpty(result.tags_json))
        {
            try
            {
                tags = JsonConvert.DeserializeObject<List<string>>(result.tags_json) ?? new List<string>();
            }
            catch
            {
                tags = new List<string>();
            }
        }

        return new EmailAccountDbModel
        {
            Id = result.id,
            AdminUuid = result.admin_uuid,
            Email = result.email,
            Name = result.name,
            Status = result.status,
            ClientId = result.client_id,
            ClientName = result.client_name,
            ClientColor = result.client_color,
            WarmupSent = result.warmup_sent,
            WarmupReplied = result.warmup_replied,
            WarmupSavedFromSpam = result.warmup_saved_from_spam,
            Sent = result.sent,
            Opened = result.opened,
            Replied = result.replied,
            Bounced = result.bounced,
            Tags = tags,
            CampaignCount = Convert.ToInt32(result.campaign_count),
            ActiveCampaignCount = Convert.ToInt32(result.active_campaign_count ?? 0),
            IsSendingActualEmails = result.is_sending_actual_emails,
            CreatedAt = result.created_at,
            UpdatedAt = result.updated_at,
            LastUpdatedAt = result.last_updated_at,
            WarmupUpdateDateTime = result.warmup_update_datetime,
            Notes = result.notes
        };
    }

    public async Task<(IEnumerable<EmailAccountDbModel> accounts, int totalCount)> GetPaginatedAsync(
        int page, 
        int pageSize, 
        string? search = null, 
        List<string>? clientIds = null,
        List<long>? emailIds = null,
        string? sortBy = null, 
        bool sortDescending = false,
        int? timeRangeDays = null,
        string? sortMode = null,
        int? minSent = null,
        string? warmupStatus = null,
        int? performanceFilterMinSent = null,
        double? performanceFilterMaxReplyRate = null)
    {
        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        // Add filters
        if (!string.IsNullOrWhiteSpace(search))
        {
            conditions.Add(@"(
                LOWER(email) LIKE @search OR 
                LOWER(name) LIKE @search OR 
                LOWER(status) LIKE @search OR 
                LOWER(client_name) LIKE @search OR 
                CAST(id AS TEXT) LIKE @search OR
                EXISTS (
                    SELECT 1 FROM jsonb_array_elements_text(tags) AS tag 
                    WHERE LOWER(tag) LIKE @search
                )
            )");
            parameters.Add("search", $"%{search.ToLower()}%");
        }

        if (clientIds != null && clientIds.Any())
        {
            // Filter email accounts by specified client IDs
            conditions.Add("client_id = ANY(@clientIds)");
            parameters.Add("clientIds", clientIds.ToArray());
        }

        if (emailIds != null && emailIds.Any())
        {
            conditions.Add("id = ANY(@emailIds)");
            parameters.Add("emailIds", emailIds.ToArray());
        }

        // Add volume filtering by minimum sent emails
        if (minSent.HasValue && minSent.Value > 0)
        {
            conditions.Add("sent >= @minSent");
            parameters.Add("minSent", minSent.Value);
        }

        // Add warmup status filtering for disconnected accounts
        if (!string.IsNullOrWhiteSpace(warmupStatus))
        {
            conditions.Add("LOWER(status) = LOWER(@warmupStatus)");
            parameters.Add("warmupStatus", warmupStatus);
        }

        // Add worst performing filter (optimized for performance)
        if (performanceFilterMinSent.HasValue && performanceFilterMaxReplyRate.HasValue)
        {
            conditions.Add("sent >= @performanceFilterMinSent");
            // Optimized: Use integer math instead of floating point division
            // Instead of: (replied/sent * 100) <= maxReplyRate
            // Use: replied * 100 <= sent * maxReplyRate (avoids division, uses integer math)
            conditions.Add("(CASE WHEN sent > 0 THEN (replied * 100) <= (sent * @performanceFilterMaxReplyRate) ELSE true END)");
            parameters.Add("performanceFilterMinSent", performanceFilterMinSent.Value);
            parameters.Add("performanceFilterMaxReplyRate", performanceFilterMaxReplyRate.Value);
        }

        var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

        // Check if we need time-range filtering (for any time range < 9999 days, not just stats sorting)
        bool needsTimeRangeStats = timeRangeDays.HasValue && timeRangeDays.Value < 9999;
        bool isPercentageMode = !string.IsNullOrWhiteSpace(sortMode) && sortMode.ToLower() == "percentage"; 
        bool isStatsSortColumn = !string.IsNullOrWhiteSpace(sortBy) && IsStatsSortColumn(sortBy.ToLower());
        
        if (needsTimeRangeStats)
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-timeRangeDays.Value);
            var endDate = DateTime.UtcNow.Date;
            parameters.Add("startDate", startDate);
            parameters.Add("endDate", endDate);
        }

        // Build ORDER BY clause with complete column mapping
        var validSortColumns = new Dictionary<string, string>
        {
            {"email", "email"},
            {"name", "name"},
            {"status", "status"},
            {"sent", "sent"},
            {"totalsent", "sent"},
            {"opened", "opened"},
            {"totalopened", "opened"},
            {"replied", "replied"},
            {"totalreplied", "replied"},
            {"bounced", "bounced"},
            {"totalbounced", "bounced"},
            {"warmupsent", "warmup_sent"},
            {"warmupreplied", "warmup_replied"},
            {"warmupsavedfromspam", "warmup_saved_from_spam"},
            {"campaigns", "campaign_count"},
            {"campaigncount", "campaign_count"},
            {"activecampaigns", "active_campaign_count"},
            {"activecampaigncount", "active_campaign_count"},
            {"issendingactualemails", "is_sending_actual_emails"},
            {"sendingactualemails", "is_sending_actual_emails"},
            {"sendingstatus", "is_sending_actual_emails"},
            {"createdat", "created_at"},
            {"updatedat", "updated_at"},
            {"lastupdatedat", "last_updated_at"},
            {"clientname", "client_name"},
            {"client", "client_name"},
            {"notes", "notes"}
        };

        var orderBy = BuildOrderByClause(sortBy, sortDescending, isPercentageMode, validSortColumns, needsTimeRangeStats);

        string countSql;
        string dataSql;
        
        if (needsTimeRangeStats)
        {
            // Use CTE to calculate time-range stats and sort by them
            var baseCte = $@"
                WITH time_range_stats AS (
                    SELECT 
                        ea.id::bigint as id,
                        ea.admin_uuid,
                        ea.email,
                        ea.name,
                        ea.status,
                        ea.client_id,
                        ea.client_name,
                        ea.client_color,
                        ea.warmup_sent,
                        ea.warmup_replied,
                        ea.warmup_saved_from_spam,
                        COALESCE(ds.sent, 0)::integer as sent,
                        COALESCE(ds.opened, 0)::integer as opened,
                        COALESCE(ds.replied, 0)::integer as replied,
                        COALESCE(ds.bounced, 0)::integer as bounced,";
                        
            var statsSubquery = @"
                        ea.tags,
                        ea.tags::text as tags_json,
                        ea.campaign_count,
                        ea.active_campaign_count,
                        ea.is_sending_actual_emails,
                        ea.created_at,
                        ea.updated_at,
                        ea.last_updated_at,
                        ea.warmup_update_datetime,
                        ea.notes
                    FROM email_accounts ea
                    LEFT JOIN (
                        SELECT 
                            email_account_id,
                            SUM(sent) as sent,
                            SUM(opened) as opened,
                            SUM(replied) as replied,
                            SUM(bounced) as bounced
                        FROM email_account_daily_stat_entries
                        WHERE stat_date >= @startDate AND stat_date <= @endDate
                        GROUP BY email_account_id
                    ) ds ON ea.id = ds.email_account_id";
                    
            countSql = $@"{baseCte}{statsSubquery}
                ) SELECT COUNT(*) FROM time_range_stats {whereClause.Replace("email_accounts", "time_range_stats")}";
                
            // Fix ORDER BY clause to work with CTE alias
            var cteOrderBy = orderBy.Replace("final.", "time_range_stats.").Replace("ea.", "time_range_stats.");
            if (!cteOrderBy.Contains("time_range_stats.") && cteOrderBy.StartsWith("ORDER BY "))
            {
                // If no table prefix was found, add the CTE alias after ORDER BY
                cteOrderBy = cteOrderBy.Replace("ORDER BY ", "ORDER BY time_range_stats.");
            }
            
            dataSql = $@"{baseCte}{statsSubquery}
                ) SELECT * FROM time_range_stats {whereClause.Replace("email_accounts", "time_range_stats")} {cteOrderBy}
                OFFSET @offset LIMIT @limit";
        }
        else
        {
            // Use regular queries for all-time stats or non-stats sorting
            countSql = $@"
                SELECT COUNT(*)
                FROM email_accounts ea
                {whereClause}";

            dataSql = $@"
                SELECT 
                    id,
                    admin_uuid,
                    email,
                    name,
                    status,
                    client_id,
                    client_name,
                    client_color,
                    warmup_sent,
                    warmup_replied,
                    warmup_saved_from_spam,
                    sent,
                    opened,
                    replied,
                    bounced,
                    tags,
                    tags::text as tags_json,
                    campaign_count,
                    active_campaign_count,
                    is_sending_actual_emails,
                    created_at,
                    updated_at,
                    last_updated_at,
                    warmup_update_datetime,
                    notes
                FROM email_accounts ea
                {whereClause}
                {orderBy}
                LIMIT @limit OFFSET @offset";
        }

        parameters.Add("limit", pageSize);
        parameters.Add("offset", (page - 1) * pageSize);

        using var connection = await _connectionService.GetConnectionAsync();
        
        var totalCount = await connection.QuerySingleAsync<int>(countSql, parameters);
        var results = await connection.QueryAsync(dataSql, parameters);
        
        return (results.Select(MapToEmailAccount), totalCount);
    }

    private static bool IsStatsSortColumn(string sortBy)
    {
        var statsColumns = new[] { "sent", "totalsent", "opened", "totalopened", "replied", "totalreplied", "bounced", "totalbounced" };
        return statsColumns.Contains(sortBy);
    }

    private static string BuildOrderByClause(string? sortBy, bool sortDescending, bool isPercentageMode, 
        Dictionary<string, string> validSortColumns, bool isTimeRangeQuery)
    {
        var defaultOrderBy = "ORDER BY created_at DESC";
        
        if (string.IsNullOrWhiteSpace(sortBy) || !validSortColumns.ContainsKey(sortBy.ToLower()))
        {
            return defaultOrderBy;
        }
        
        var column = validSortColumns[sortBy.ToLower()];
        var direction = sortDescending ? "DESC" : "ASC";
        var sortColumn = sortBy.ToLower();
        
        // Add table alias prefix for consistency
        var tablePrefix = isTimeRangeQuery ? "final." : "ea.";
        
        // Handle percentage mode for statistics columns
        if (isPercentageMode && IsStatsSortColumn(sortColumn))
        {
            var numeratorCol = $"{tablePrefix}{column}";
            var denominatorCol = $"{tablePrefix}sent";
            
            // Special cases for warmup percentages
            if (sortColumn.Contains("warmup"))
            {
                denominatorCol = $"{tablePrefix}warmup_sent";
            }
            
            // Build percentage calculation with proper NULL handling
            var percentageExpr = $"CASE WHEN {denominatorCol} > 0 THEN CAST({numeratorCol} AS DECIMAL) / {denominatorCol} ELSE 0 END";
            return $"ORDER BY {percentageExpr} {direction}";
        }
        
        // Special handling for complex columns
        switch (sortColumn)
        {
            case "lastupdatedat":
                return $"ORDER BY COALESCE({tablePrefix}last_updated_at, {tablePrefix}updated_at) {direction}";
                
            case "issendingactualemails":
            case "sendingactualemails":
            case "sendingstatus":
                // For DESC: true first, then false, then null
                // For ASC: null first, then false, then true
                var nullsClause = sortDescending ? "NULLS LAST" : "NULLS FIRST";
                return $"ORDER BY {tablePrefix}{column} {direction} {nullsClause}";
                
            case "tags":
                // Sort by concatenated tag string
                return $"ORDER BY COALESCE(array_to_string(ARRAY(SELECT jsonb_array_elements_text({tablePrefix}tags) ORDER BY 1), ', '), '') {direction}";
                
            case "tagcount":
                // Sort by number of tags
                return $"ORDER BY COALESCE(jsonb_array_length({tablePrefix}tags), 0) {direction}";
                
            case "clientname":
            case "client":
                // Sort by client name with proper NULL handling for alphabetical order
                // ASC: A,B,C...,Z,NULL (nulls last) | DESC: NULL,Z,Y,...,B,A (nulls first)
                var nullsPosition = sortDescending ? "NULLS FIRST" : "NULLS LAST";
                return $"ORDER BY {tablePrefix}{column} {direction} {nullsPosition}";
                
            case "updatedat":
            case "createdat":
                // Add secondary sort by id for consistent ordering when timestamps are identical
                return $"ORDER BY {tablePrefix}{column} {direction}, {tablePrefix}id {direction}";
                
            default:
                return $"ORDER BY {tablePrefix}{column} {direction}";
        }
    }

    public async Task<Dictionary<long, int>> GetCampaignCountsAsync(List<long> emailAccountIds)
    {
        if (!emailAccountIds?.Any() ?? true)
            return new Dictionary<long, int>();

        const string sql = @"
            SELECT 
                ea.id,
                COALESCE(campaign_counts.count, 0) as campaign_count
            FROM email_accounts ea
            LEFT JOIN (
                SELECT 
                    email_id,
                    COUNT(*) as count
                FROM (
                    SELECT (jsonb_array_elements(email_ids))::bigint as email_id
                    FROM campaigns
                    WHERE email_ids IS NOT NULL AND jsonb_array_length(email_ids) > 0
                ) email_campaigns
                GROUP BY email_id
            ) campaign_counts ON ea.id = campaign_counts.email_id
            WHERE ea.id = ANY(@emailAccountIds)";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync<(long id, int campaign_count)>(
            sql, 
            new { emailAccountIds = emailAccountIds.ToArray() }
        );
        
        return results.ToDictionary(r => r.id, r => r.campaign_count);
    }
    
    public async Task<IEnumerable<EmailAccountDbModel>> GetAccountsNeedingWarmupUpdateAsync(string adminUuid, int minutesSinceLastUpdate = 1440)
    {
        const string sql = @"
            SELECT 
                id,
                admin_uuid,
                email,
                name,
                status,
                client_id,
                client_name,
                client_color,
                warmup_sent,
                warmup_replied,
                warmup_saved_from_spam,
                sent,
                opened,
                replied,
                bounced,
                tags,
                campaign_count,
                created_at,
                updated_at,
                last_updated_at,
                warmup_update_datetime,
                notes
            FROM email_accounts
            WHERE admin_uuid = @AdminUuid
                AND (warmup_update_datetime IS NULL 
                     OR warmup_update_datetime < NOW() - MAKE_INTERVAL(mins => @MinutesSinceLastUpdate))
            ORDER BY warmup_update_datetime ASC NULLS FIRST";

        using var connection = await _connectionService.GetConnectionAsync();
        var results = await connection.QueryAsync(sql, new { AdminUuid = adminUuid, MinutesSinceLastUpdate = minutesSinceLastUpdate });
        
        return results.Select(r => 
        {
            var tags = new List<string>();
            if (!string.IsNullOrEmpty(r.tags))
            {
                try
                {
                    tags = JsonConvert.DeserializeObject<List<string>>(r.tags.ToString()) ?? new List<string>();
                }
                catch
                {
                    tags = new List<string>();
                }
            }
            
            return new EmailAccountDbModel
            {
                Id = r.id,
                AdminUuid = r.admin_uuid,
                Email = r.email,
                Name = r.name,
                Status = r.status,
                ClientId = r.client_id,
                ClientName = r.client_name,
                ClientColor = r.client_color,
                WarmupSent = r.warmup_sent,
                WarmupReplied = r.warmup_replied,
                WarmupSavedFromSpam = r.warmup_saved_from_spam,
                Sent = r.sent,
                Opened = r.opened,
                Replied = r.replied,
                Bounced = r.bounced,
                Tags = tags,
                CampaignCount = r.campaign_count,
                ActiveCampaignCount = r.active_campaign_count ?? 0,
                IsSendingActualEmails = r.is_sending_actual_emails,
                CreatedAt = r.created_at,
                UpdatedAt = r.updated_at,
                LastUpdatedAt = r.last_updated_at,
                WarmupUpdateDateTime = r.warmup_update_datetime,
                Notes = r.notes
            };
        });
    }
}