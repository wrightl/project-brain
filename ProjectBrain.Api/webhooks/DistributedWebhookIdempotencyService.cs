using System.Text;
using Microsoft.Extensions.Caching.Distributed;

namespace ProjectBrain.Api.Webhooks;

public class DistributedWebhookIdempotencyService(IDistributedCache cache) : IWebhookIdempotencyService
{
    private static readonly byte[] ProcessedMarker = Encoding.UTF8.GetBytes("1");

    public bool HasProcessed(string provider, string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return false;
        }

        var cacheKey = $"webhook:{provider}:{eventId}";
        return cache.Get(cacheKey) is not null;
    }

    public void MarkProcessed(string provider, string eventId, TimeSpan ttl)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return;
        }

        var cacheKey = $"webhook:{provider}:{eventId}";
        cache.Set(
            cacheKey,
            ProcessedMarker,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            });
    }
}
