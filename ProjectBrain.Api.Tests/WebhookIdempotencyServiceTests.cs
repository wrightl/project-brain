using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using ProjectBrain.Api.Webhooks;

namespace ProjectBrain.Api.Tests;

public class WebhookIdempotencyServiceTests
{
    [Fact]
    public void TryBeginProcessing_AfterMarkProcessed_BlocksDuplicate()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new WebhookIdempotencyService(cache);

        service.TryBeginProcessing("auth0", "evt-1", TimeSpan.FromMinutes(5)).Should().BeTrue();
        service.MarkProcessed("auth0", "evt-1", TimeSpan.FromMinutes(5));

        service.TryBeginProcessing("auth0", "evt-1", TimeSpan.FromMinutes(5)).Should().BeFalse();
    }

    [Fact]
    public void TryBeginProcessing_AfterClearingProcessing_AllowsRetry()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new WebhookIdempotencyService(cache);

        service.TryBeginProcessing("stripe", "evt-2", TimeSpan.FromMinutes(5)).Should().BeTrue();
        service.TryBeginProcessing("stripe", "evt-2", TimeSpan.FromMinutes(5)).Should().BeFalse();

        service.ClearProcessing("stripe", "evt-2");

        service.TryBeginProcessing("stripe", "evt-2", TimeSpan.FromMinutes(5)).Should().BeTrue();
    }
}
