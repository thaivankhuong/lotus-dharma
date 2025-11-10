namespace LotusDharma.Application.Common.Caching;

/// <summary>
/// Configuration options for distributed caching.
/// Optimized for 100K-5M concurrent users.
/// </summary>
public class CacheOptions
{
    public const string SectionName = "CacheSettings";

    /// <summary>
    /// Default cache expiration time. Default: 1 hour.
    /// </summary>
    public int DefaultExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Enable cache compression for objects > 1KB. 
    /// Reduces network bandwidth by 60-80%.
    /// </summary>
    public bool EnableCompression { get; set; } = true;

    /// <summary>
    /// Compression threshold in bytes. Default: 1024 (1KB).
    /// </summary>
    public int CompressionThresholdBytes { get; set; } = 1024;

    /// <summary>
    /// Enable circuit breaker to handle Redis failures gracefully.
    /// App continues working even if Redis is down.
    /// </summary>
    public bool EnableCircuitBreaker { get; set; } = true;

    /// <summary>
    /// Number of failures before opening circuit. Default: 5.
    /// </summary>
    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    /// <summary>
    /// Time to wait before trying Redis again after circuit opens. Default: 30s.
    /// </summary>
    public int CircuitBreakerDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Enable detailed metrics collection (hit/miss rates, latency).
    /// Minimal performance impact.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Key prefix for all cache entries. Useful for multi-tenant scenarios.
    /// </summary>
    public string KeyPrefix { get; set; } = "ca:"; // LotusDharma prefix

    /// <summary>
    /// Specific expiration times for different entity types (in minutes).
    /// </summary>
    public EntityExpirationOptions EntityExpiration { get; set; } = new();
}

public class EntityExpirationOptions
{
    /// <summary>
    /// Cache duration for categories (rarely change). Default: 12 hours.
    /// </summary>
    public int CategoriesMinutes { get; set; } = 720;

    /// <summary>
    /// Cache duration for products. Default: 1 hour.
    /// </summary>
    public int ProductsMinutes { get; set; } = 60;

    /// <summary>
    /// Cache duration for user-specific data. Default: 30 minutes.
    /// </summary>
    public int UserDataMinutes { get; set; } = 30;

    /// <summary>
    /// Cache duration for search results. Default: 15 minutes.
    /// </summary>
    public int SearchResultsMinutes { get; set; } = 15;

    /// <summary>
    /// Cache duration for aggregated stats. Default: 5 minutes.
    /// </summary>
    public int StatisticsMinutes { get; set; } = 5;
}

