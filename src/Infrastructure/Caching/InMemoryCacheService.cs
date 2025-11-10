using System.Collections.Concurrent;
using LotusDharma.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LotusDharma.Infrastructure.Caching;

/// <summary>
/// In-memory cache service for development or fallback when Redis is unavailable.
/// WARNING: Not suitable for production with multiple instances (no distributed caching).
/// Data is lost when app restarts.
/// </summary>
public class InMemoryCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ILogger<InMemoryCacheService> _logger;

    public InMemoryCacheService(ILogger<InMemoryCacheService> logger)
    {
        _logger = logger;
        _logger.LogWarning("Using InMemoryCacheService. This is NOT suitable for production with multiple instances!");
        
        // Background cleanup task
        _ = Task.Run(CleanupExpiredEntriesAsync);
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (entry.ExpiresAt > DateTime.UtcNow)
            {
                return Task.FromResult(entry.Value as T);
            }
            else
            {
                // Remove expired entry
                _cache.TryRemove(key, out _);
            }
        }

        return Task.FromResult<T?>(null);
    }

    public Task SetAsync<T>(
        string key, 
        T value, 
        TimeSpan? expiry = null, 
        CancellationToken cancellationToken = default) where T : class
    {
        var expiresAt = DateTime.UtcNow.Add(expiry ?? TimeSpan.FromHours(1));
        var entry = new CacheEntry(value, expiresAt);
        
        _cache[key] = entry;
        
        return Task.CompletedTask;
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key, 
        Func<Task<T>> factory, 
        TimeSpan? expiry = null, 
        CancellationToken cancellationToken = default) where T : class
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached != null)
            return cached;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Double-check
            cached = await GetAsync<T>(key, cancellationToken);
            if (cached != null)
                return cached;

            var value = await factory();
            await SetAsync(key, value, expiry, cancellationToken);
            return value;
        }
        finally
        {
            _lock.Release();
        }
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // Simple pattern matching (supports wildcards *)
        var normalizedPattern = pattern.Replace("*", "");
        var keysToRemove = _cache.Keys
            .Where(k => k.Contains(normalizedPattern))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            return Task.FromResult(entry.ExpiresAt > DateTime.UtcNow);
        }

        return Task.FromResult(false);
    }

    public Task RefreshAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            var newEntry = new CacheEntry(entry.Value, DateTime.UtcNow.Add(expiry));
            _cache[key] = newEntry;
        }

        return Task.CompletedTask;
    }

    private async Task CleanupExpiredEntriesAsync()
    {
        while (true)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(5));

                var expiredKeys = _cache
                    .Where(kvp => kvp.Value.ExpiresAt <= DateTime.UtcNow)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    _cache.TryRemove(key, out _);
                }

                if (expiredKeys.Any())
                {
                    _logger.LogDebug("Cleaned up {Count} expired cache entries", expiredKeys.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache cleanup");
            }
        }
    }

    private record CacheEntry(object Value, DateTime ExpiresAt);
}

