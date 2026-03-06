using System.IO.Compression;
using System.Text.Json;
using LotusDharma.Application.Common.Caching;
using LotusDharma.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace LotusDharma.Infrastructure.Caching;

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly CacheOptions _options;

    // Circuit breaker state
    private int _consecutiveFailures;
    private DateTime _circuitOpenedAt = DateTime.MinValue;

    // Metrics
    private long _hits;
    private long _misses;
    private long _errors;

    public RedisCacheService(
        IConnectionMultiplexer redis,
        IOptions<CacheOptions> options,
        ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _db = redis.GetDatabase();
        _logger = logger;
        _options = options.Value;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (IsCircuitOpen()) return null;

        try
        {
            var prefixedKey = GetPrefixedKey(key);
            var value = await _db.StringGetAsync(prefixedKey);

            if (value.IsNullOrEmpty)
            {
                Interlocked.Increment(ref _misses);
                return null;
            }

            Interlocked.Increment(ref _hits);
            RecordSuccess();
            return DeserializeValue<T>(value!);
        }
        catch (Exception ex)
        {
            RecordFailure(ex, "GET", key);
            return null;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default) where T : class
    {
        if (IsCircuitOpen()) return;

        try
        {
            var prefixedKey = GetPrefixedKey(key);
            var serialized = SerializeValue(value);
            var expiryTime = expiry ?? TimeSpan.FromMinutes(_options.DefaultExpirationMinutes);

            await _db.StringSetAsync(prefixedKey, serialized, expiryTime);
            RecordSuccess();
        }
        catch (Exception ex)
        {
            RecordFailure(ex, "SET", key);
        }
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default) where T : class
    {
        if (!IsCircuitOpen())
        {
            var cached = await GetAsync<T>(key, cancellationToken);
            if (cached != null)
                return cached;
        }

        var lockKey = $"lock:{key}";
        var lockValue = Guid.NewGuid().ToString();
        var lockExpiry = TimeSpan.FromSeconds(10);

        try
        {
            if (IsCircuitOpen())
                return await factory();

            var lockAcquired = await _db.StringSetAsync(
                GetPrefixedKey(lockKey),
                lockValue,
                lockExpiry,
                When.NotExists
            );

            if (lockAcquired)
            {
                try
                {
                    var cached = await GetAsync<T>(key, cancellationToken);
                    if (cached != null)
                        return cached;

                    var value = await factory();
                    await SetAsync(key, value, expiry, cancellationToken);
                    return value;
                }
                finally
                {
                    await ReleaseLockAsync(lockKey, lockValue);
                }
            }
            else
            {
                await Task.Delay(100, cancellationToken);

                var cached = await GetAsync<T>(key, cancellationToken);
                if (cached != null)
                    return cached;

                return await factory();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetOrCreate error for key: {Key}", key);
            return await factory();
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (IsCircuitOpen()) return;

        try
        {
            var prefixedKey = GetPrefixedKey(key);
            await _db.KeyDeleteAsync(prefixedKey);
            RecordSuccess();
        }
        catch (Exception ex)
        {
            RecordFailure(ex, "REMOVE", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        if (IsCircuitOpen()) return;

        try
        {
            var prefixedPattern = GetPrefixedKey(pattern);
            var endpoints = _redis.GetEndPoints();
            var server = _redis.GetServer(endpoints.First());

            var keys = server.Keys(pattern: prefixedPattern, pageSize: 1000)
                .Take(10000)
                .ToArray();

            if (keys.Length > 0)
            {
                await _db.KeyDeleteAsync(keys);
                _logger.LogInformation("Cache REMOVE by pattern: {Pattern}, Count: {Count}", pattern, keys.Length);
            }

            RecordSuccess();
        }
        catch (Exception ex)
        {
            RecordFailure(ex, "REMOVE_PATTERN", pattern);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (IsCircuitOpen()) return false;

        try
        {
            var prefixedKey = GetPrefixedKey(key);
            var result = await _db.KeyExistsAsync(prefixedKey);
            RecordSuccess();
            return result;
        }
        catch (Exception ex)
        {
            RecordFailure(ex, "EXISTS", key);
            return false;
        }
    }

    public async Task RefreshAsync(string key, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        if (IsCircuitOpen()) return;

        try
        {
            var prefixedKey = GetPrefixedKey(key);
            await _db.KeyExpireAsync(prefixedKey, expiry);
            RecordSuccess();
        }
        catch (Exception ex)
        {
            RecordFailure(ex, "REFRESH", key);
        }
    }

    private bool IsCircuitOpen()
    {
        if (!_options.EnableCircuitBreaker) return false;

        if (_consecutiveFailures >= _options.CircuitBreakerFailureThreshold)
        {
            var elapsed = DateTime.UtcNow - _circuitOpenedAt;
            if (elapsed.TotalSeconds < _options.CircuitBreakerDurationSeconds)
            {
                return true;
            }

            // Half-open: allow one attempt through
            Interlocked.Exchange(ref _consecutiveFailures, _options.CircuitBreakerFailureThreshold - 1);
        }

        return false;
    }

    private void RecordSuccess()
    {
        Interlocked.Exchange(ref _consecutiveFailures, 0);
    }

    private void RecordFailure(Exception ex, string operation, string key)
    {
        Interlocked.Increment(ref _errors);
        var failures = Interlocked.Increment(ref _consecutiveFailures);

        if (failures == _options.CircuitBreakerFailureThreshold)
        {
            _circuitOpenedAt = DateTime.UtcNow;
            _logger.LogWarning("Redis circuit breaker OPENED after {Count} consecutive failures", failures);
        }

        _logger.LogError(ex, "Redis {Operation} error for key: {Key}", operation, key);
    }

    private string GetPrefixedKey(string key) => $"{_options.KeyPrefix}{key}";

    private async Task ReleaseLockAsync(string lockKey, string lockValue)
    {
        try
        {
            var script = @"
                if redis.call('get', KEYS[1]) == ARGV[1] then
                    return redis.call('del', KEYS[1])
                else
                    return 0
                end";

            await _db.ScriptEvaluateAsync(
                script,
                new RedisKey[] { GetPrefixedKey(lockKey) },
                new RedisValue[] { lockValue }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing lock: {LockKey}", lockKey);
        }
    }

    private byte[] SerializeValue<T>(T value)
    {
        var json = JsonSerializer.Serialize(value);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);

        if (_options.EnableCompression && bytes.Length > _options.CompressionThresholdBytes)
        {
            return Compress(bytes);
        }

        return bytes;
    }

    private T? DeserializeValue<T>(byte[] bytes)
    {
        if (_options.EnableCompression)
        {
            try
            {
                bytes = Decompress(bytes);
            }
            catch
            {
                // Not compressed, use as-is
            }
        }

        var json = System.Text.Encoding.UTF8.GetString(bytes);
        return JsonSerializer.Deserialize<T>(json);
    }

    private static byte[] Compress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest))
        {
            gzip.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    private static byte[] Decompress(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }

    public (long Hits, long Misses, long Errors, double HitRate) GetMetrics()
    {
        var total = _hits + _misses;
        var hitRate = total == 0 ? 0 : (double)_hits / total;
        return (_hits, _misses, _errors, hitRate);
    }
}
