using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ProjectBrain.Api.Webhooks;

namespace ProjectBrain.Api.Tests;

public class WebhookIdempotencyServiceTests
{
    private static DistributedWebhookIdempotencyService CreateService() =>
        new(new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())));

    [Fact]
    public void TryClaim_NewEvent_ReturnsClaimed()
    {
        var service = CreateService();

        service.TryClaim("stripe", "evt_1").Should().Be(WebhookClaimResult.Claimed);
    }

    [Fact]
    public void TryClaim_WhileFirstDeliveryInProgress_ReturnsInProgress()
    {
        var service = CreateService();
        service.TryClaim("stripe", "evt_1");

        service.TryClaim("stripe", "evt_1").Should().Be(WebhookClaimResult.InProgress);
    }

    [Fact]
    public void TryClaim_AfterMarkProcessed_ReturnsAlreadyProcessed()
    {
        var service = CreateService();
        service.TryClaim("stripe", "evt_1");
        service.MarkProcessed("stripe", "evt_1", TimeSpan.FromDays(7));

        service.TryClaim("stripe", "evt_1").Should().Be(WebhookClaimResult.AlreadyProcessed);
    }

    [Fact]
    public void TryClaim_AfterAbandon_AllowsRetryToBeClaimed()
    {
        var service = CreateService();
        service.TryClaim("stripe", "evt_1");
        service.Abandon("stripe", "evt_1");

        service.TryClaim("stripe", "evt_1").Should().Be(WebhookClaimResult.Claimed);
    }

    [Fact]
    public void TryClaim_SameEventIdFromDifferentProviders_AreIndependent()
    {
        var service = CreateService();
        service.TryClaim("stripe", "evt_1");
        service.MarkProcessed("stripe", "evt_1", TimeSpan.FromDays(7));

        service.TryClaim("auth0", "evt_1").Should().Be(WebhookClaimResult.Claimed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void TryClaim_MissingEventId_AlwaysClaims(string? eventId)
    {
        var service = CreateService();
        service.TryClaim("stripe", eventId);
        service.MarkProcessed("stripe", eventId, TimeSpan.FromDays(7));

        service.TryClaim("stripe", eventId).Should().Be(WebhookClaimResult.Claimed);
    }

    [Fact]
    public async Task TryClaim_ConcurrentDeliveries_OnlyOneClaims()
    {
        var service = CreateService();

        var results = await Task.WhenAll(
            Enumerable.Range(0, 20).Select(_ => Task.Run(() => service.TryClaim("stripe", "evt_1"))));

        results.Count(r => r == WebhookClaimResult.Claimed).Should().Be(1);
    }
}
