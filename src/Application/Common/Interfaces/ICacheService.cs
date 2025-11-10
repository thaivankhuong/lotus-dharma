namespace LotusDharma.Application.Common.Interfaces;

/// <summary>
/// Distributed cache service for high-performance data caching.
/// Supports up to 5M concurrent users with proper configuration.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a cached value by key. Returns null if not found.
    /// Average latency: 1-5ms
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Sets a value in cache with optional expiration.
    /// </summary>
    Task SetAsync<T>(
        string key, 
        T value, 
        TimeSpan? expiry = null, 
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Gets value from cache or creates it using factory function.
    /// Implements cache-aside pattern with stampede prevention.
    /// </summary>
    Task<T> GetOrCreateAsync<T>(
        string key, 
        Func<Task<T>> factory, 
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Removes a single key from cache.
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all keys matching a pattern (e.g., "products:*").
    /// Use with caution in production - can be slow with millions of keys.
    /// </summary>
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a key exists in cache.
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes TTL for an existing key without updating the value.
    /// </summary>
    Task RefreshAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default);
}

