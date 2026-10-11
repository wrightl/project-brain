using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ProjectBrain.Api.Hubs;

namespace ProjectBrain.Api.Tests;

public class HubTicketServiceTests
{
    private static HubTicketService CreateService() =>
        new(new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())));

    [Fact]
    public async Task RedeemAsync_IssuedTicket_ReturnsPrincipalForUser()
    {
        var service = CreateService();
        var ticket = await service.IssueAsync("auth0|user-1");

        var principal = await service.RedeemAsync(ticket.Ticket);

        principal.Should().NotBeNull();
        principal!.FindFirst(ClaimTypes.NameIdentifier)!.Value.Should().Be("auth0|user-1");
        principal.Identity!.IsAuthenticated.Should().BeTrue();
        principal.Identity.AuthenticationType.Should().Be(HubTicketService.AuthenticationType);
    }

    [Fact]
    public async Task RedeemAsync_TicketCanBeUsedForNegotiateAndConnect()
    {
        var service = CreateService();
        var ticket = await service.IssueAsync("auth0|user-1");

        (await service.RedeemAsync(ticket.Ticket)).Should().NotBeNull();
        (await service.RedeemAsync(ticket.Ticket)).Should().NotBeNull();
    }

    [Fact]
    public async Task RedeemAsync_UnknownTicket_ReturnsNull()
    {
        var service = CreateService();

        (await service.RedeemAsync(HubTicketService.TicketPrefix + "unknown")).Should().BeNull();
    }

    [Fact]
    public async Task RedeemAsync_JwtInsteadOfTicket_ReturnsNull()
    {
        var service = CreateService();

        (await service.RedeemAsync("eyJhbGciOi.eyJzdWIiOi.signature")).Should().BeNull();
    }

    [Fact]
    public async Task IssueAsync_ReturnsDistinctPrefixedTickets()
    {
        var service = CreateService();

        var first = await service.IssueAsync("auth0|user-1");
        var second = await service.IssueAsync("auth0|user-1");

        first.Ticket.Should().StartWith(HubTicketService.TicketPrefix);
        first.Ticket.Should().NotBe(second.Ticket);
        first.ExpiresInSeconds.Should().Be(60);
    }
}
