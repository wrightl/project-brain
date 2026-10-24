using System.Text;
using Microsoft.Extensions.Caching.Distributed;

namespace ProjectBrain.Api.Webhooks;

public class DistributedWebhookIdempotencyService(IDistributedCache cache) : IWebhookIdempotencyService
{
    // A claim that is never completed (e.g. the process dies mid-handler) expires so a later retry can proceed.
    private static readonly TimeSpan InProgressTtl = TimeSpan.FromMinutes(5);
    private static readonly byte[] InProgressMarker = Encoding.UTF8.GetBytes("in-progress");
    private static readonly byte[] ProcessedMarker = Encoding.UTF8.GetBytes("processed");

    // IDistributedCache has no compare-and-set; serialise claims within this (single-replica) process.
    private readonly Lock _claimLock = new();

    public WebhookClaimResult TryClaim(string provider, string? eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return WebhookClaimResult.Claimed;
        }

        var cacheKey = GetCacheKey(provider, eventId);
        lock (_claimLock)
        {
            var existing = cache.Get(cacheKey);
            if (existing is not null)
            {
                return existing.AsSpan().SequenceEqual(ProcessedMarker)
                    ? WebhookClaimResult.AlreadyProcessed
                    : WebhookClaimResult.InProgress;
            }

            cache.Set(cacheKey, InProgressMarker, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = InProgressTtl
            });
            return WebhookClaimResult.Claimed;
        }
    }

    public void MarkProcessed(string provider, string? eventId, TimeSpan ttl)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return;
        }

        cache.Set(GetCacheKey(provider, eventId), ProcessedMarker, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        });
    }

    public void Abandon(string provider, string? eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return;
        }

        cache.Remove(GetCacheKey(provider, eventId));
    }

    private static string GetCacheKey(string provider, string eventId) => $"webhook:{provider}:{eventId}";
}
