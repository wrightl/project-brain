using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace ProjectBrain.Api.Webhooks;

public interface IWebhookIdempotencyService
{
    bool TryMarkProcessed(string provider, string eventId, TimeSpan ttl);
}

public class WebhookIdempotencyService(IMemoryCache cache) : IWebhookIdempotencyService
{
    public bool TryMarkProcessed(string provider, string eventId, TimeSpan ttl)
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

        cache.Set(cacheKey, true, ttl);
        return true;
    }
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
