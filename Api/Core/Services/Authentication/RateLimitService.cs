using System.Collections.Concurrent;

namespace LeadHype.Api.Services
{
    public interface IRateLimitService
    {
        Task<bool> IsAllowedAsync(string identifier, int maxAttempts = 5, TimeSpan? timeWindow = null);
        Task ResetAsync(string identifier);
    }

    public class RateLimitService : IRateLimitService
    {
        private readonly ConcurrentDictionary<string, RateLimitEntry> _rateLimits;
        private readonly Timer _cleanupTimer;

        public RateLimitService()
        {
            _rateLimits = new ConcurrentDictionary<string, RateLimitEntry>();
            
            // Cleanup expired entries every 5 minutes
            _cleanupTimer = new Timer(CleanupExpiredEntries, null, 
                TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        public Task<bool> IsAllowedAsync(string identifier, int maxAttempts = 5, TimeSpan? timeWindow = null)
        {
            var window = timeWindow ?? TimeSpan.FromMinutes(15); // Default 15 minutes
            var now = DateTime.UtcNow;
            
            var entry = _rateLimits.AddOrUpdate(identifier, 
                new RateLimitEntry { Count = 1, FirstAttempt = now, LastAttempt = now },
                (key, existing) =>
                {
                    // If the time window has passed, reset the counter
                    if (now - existing.FirstAttempt > window)
                    {
                        return new RateLimitEntry { Count = 1, FirstAttempt = now, LastAttempt = now };
                    }
                    
                    // Increment the counter
                    existing.Count++;
                    existing.LastAttempt = now;
                    return existing;
                });

            return Task.FromResult(entry.Count <= maxAttempts);
        }

        public Task ResetAsync(string identifier)
        {
            _rateLimits.TryRemove(identifier, out _);
            return Task.CompletedTask;
        }

        private void CleanupExpiredEntries(object? state)
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _rateLimits
                .Where(kvp => now - kvp.Value.LastAttempt > TimeSpan.FromHours(1))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _rateLimits.TryRemove(key, out _);
            }
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
        }
    }

    public class RateLimitEntry
    {
        public int Count { get; set; }
        public DateTime FirstAttempt { get; set; }
        public DateTime LastAttempt { get; set; }
    }
}