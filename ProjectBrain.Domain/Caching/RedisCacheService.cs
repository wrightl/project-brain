namespace ProjectBrain.Domain.Caching;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

/// <summary>
/// Redis-based cache service implementation
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        string? cachedValue;
        try
        {
            cachedValue = await _cache.GetStringAsync(key, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Cache get operation cancelled for key {CacheKey}", key);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache get failed for key {CacheKey}", key);
            return null;
        }

        if (string.IsNullOrEmpty(cachedValue))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(cachedValue, _jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Cache deserialization failed for key {CacheKey} and type {CacheType}. Removing invalid cache entry.", key, typeof(T).Name);

            // If deserialization fails, remove the invalid cache entry.
            await RemoveAsync(key, cancellationToken);
            return null;
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Cache deserialization operation cancelled for key {CacheKey}", key);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected cache deserialization error for key {CacheKey} and type {CacheType}", key, typeof(T).Name);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            var options = new DistributedCacheEntryOptions();

            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiration.Value;
            }
            else
            {
                // Default expiration: 1 hour
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            }

            await _cache.SetStringAsync(key, json, options, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Cache set operation cancelled for key {CacheKey}", key);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache set failed for key {CacheKey} and type {CacheType}", key, typeof(T).Name);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Cache remove operation cancelled for key {CacheKey}", key);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache remove failed for key {CacheKey}", key);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _cache.GetStringAsync(key, cancellationToken);
            return !string.IsNullOrEmpty(value);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Cache exists operation cancelled for key {CacheKey}", key);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache exists check failed for key {CacheKey}", key);
            return false;
        }
    }
}

