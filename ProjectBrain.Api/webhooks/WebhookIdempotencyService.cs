using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace ProjectBrain.Api.Webhooks;

public interface IWebhookIdempotencyService
{
    bool HasProcessed(string provider, string eventId);
    void MarkProcessed(string provider, string eventId, TimeSpan ttl);
}

public class WebhookIdempotencyService(IMemoryCache cache) : IWebhookIdempotencyService
{
    public bool HasProcessed(string provider, string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return false;
        }

        var cacheKey = $"webhook:{provider}:{eventId}";
        return cache.TryGetValue(cacheKey, out _);
    }

    public void MarkProcessed(string provider, string eventId, TimeSpan ttl)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return;
        }

        var cacheKey = $"webhook:{provider}:{eventId}";
        cache.Set(cacheKey, true, ttl);
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

    public static string ComputeSha256Hex(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash);
    }
}
