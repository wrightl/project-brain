using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Distributed;

namespace ProjectBrain.Api.Hubs;

/// <summary>
/// Short-lived opaque tickets that let the web portal open SignalR connections without handing
/// the user's API access token to browser JavaScript. A ticket only authenticates requests to /hubs.
/// </summary>
public interface IHubTicketService
{
    Task<HubTicket> IssueAsync(string userId, CancellationToken cancellationToken = default);
    Task<ClaimsPrincipal?> RedeemAsync(string ticket, CancellationToken cancellationToken = default);
}

public record HubTicket(string Ticket, int ExpiresInSeconds);

public class HubTicketService(IDistributedCache cache) : IHubTicketService
{
    public const string TicketPrefix = "hubt_";
    public const string AuthenticationType = "HubTicket";

    // Long enough for negotiate plus the transport connect, which both present the ticket.
    private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(60);

    public static bool IsTicket(string? token) =>
        token is not null && token.StartsWith(TicketPrefix, StringComparison.Ordinal);

    public async Task<HubTicket> IssueAsync(string userId, CancellationToken cancellationToken = default)
    {
        var ticket = TicketPrefix + WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

        await cache.SetStringAsync(GetCacheKey(ticket), userId, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = Lifetime
        }, cancellationToken);

        return new HubTicket(ticket, (int)Lifetime.TotalSeconds);
    }

    public async Task<ClaimsPrincipal?> RedeemAsync(string ticket, CancellationToken cancellationToken = default)
    {
        if (!IsTicket(ticket))
        {
            return null;
        }

        var userId = await cache.GetStringAsync(GetCacheKey(ticket), cancellationToken);
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId)],
            AuthenticationType);
        return new ClaimsPrincipal(identity);
    }

    private static string GetCacheKey(string ticket) => $"hub-ticket:{ticket}";
}

public static class HubTicketEndpoints
{
    public static void MapHubTicketEndpoints(this WebApplication app)
    {
        app.MapPost("/hubs/tickets", IssueTicket)
            .WithName("IssueHubTicket")
            .RequireAuthorization();
    }

    private static async Task<IResult> IssueTicket(
        HttpContext httpContext,
        IHubTicketService hubTicketService,
        CancellationToken cancellationToken)
    {
        // A ticket must not be able to mint further tickets; only a real access token can.
        if (httpContext.User.Identity?.AuthenticationType == HubTicketService.AuthenticationType)
        {
            return Results.Forbid();
        }

        var userId = httpContext.User.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        var ticket = await hubTicketService.IssueAsync(userId, cancellationToken);
        return Results.Ok(new { ticket = ticket.Ticket, expiresIn = ticket.ExpiresInSeconds });
    }
}
