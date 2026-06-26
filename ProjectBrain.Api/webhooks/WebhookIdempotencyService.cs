using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace ProjectBrain.Api.Webhooks;

public interface IWebhookIdempotencyService
{
    bool TryBeginProcessing(string provider, string eventId, TimeSpan ttl);
    void MarkProcessed(string provider, string eventId, TimeSpan ttl);
    void ClearProcessing(string provider, string eventId);
}

public class WebhookIdempotencyService(IMemoryCache cache) : IWebhookIdempotencyService
{
    private const string ProcessingState = "processing";
    private const string ProcessedState = "processed";

    public bool TryBeginProcessing(string provider, string eventId, TimeSpan ttl)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return true;
        }

        var cacheKey = $"webhook:{provider}:{eventId}";
        if (cache.TryGetValue(cacheKey, out _))
        {
            return false;
        }

        cache.Set(cacheKey, ProcessingState, ttl);
        return true;
    }

    public void MarkProcessed(string provider, string eventId, TimeSpan ttl)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return;
        }

        cache.Set(GetCacheKey(provider, eventId), ProcessedState, ttl);
    }

    public void ClearProcessing(string provider, string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return;
        }

        var cacheKey = GetCacheKey(provider, eventId);
        if (cache.TryGetValue(cacheKey, out string? state) && state == ProcessingState)
        {
            cache.Remove(cacheKey);
        }
    }

    private static string GetCacheKey(string provider, string eventId) => $"webhook:{provider}:{eventId}";
}

public static class WebhookSecurity
{
    public static bool IsValidBearerToken(string? providedToken, string? expectedToken)
    {
        if (string.IsNullOrEmpty(providedToken) || string.IsNullOrEmpty(expectedToken))
        {
            return false;
        }

        var providedBytes = Encoding.UTF8.GetBytes(providedToken);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedToken);

        return providedBytes.Length == expectedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}
