using System.Text;
using Microsoft.Extensions.Caching.Distributed;

namespace ProjectBrain.Api.Webhooks;

public class DistributedWebhookIdempotencyService(IDistributedCache cache) : IWebhookIdempotencyService
{
    private static readonly byte[] ProcessedMarker = Encoding.UTF8.GetBytes("1");

    public bool TryMarkProcessed(string provider, string eventId, TimeSpan ttl)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return true;
        }

        var cacheKey = $"webhook:{provider}:{eventId}";
        if (cache.Get(cacheKey) is not null)
        {
            return false;
        }

        cache.Set(
            cacheKey,
            ProcessedMarker,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            });

        return true;
    }
}
