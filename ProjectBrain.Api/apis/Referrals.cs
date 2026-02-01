using Microsoft.AspNetCore.Mvc;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain;
using System;
using System.Linq;

public class ReferralServices(
    ILogger<ReferralServices> logger,
    IIdentityService identityService,
    IReferralSettingsService referralSettingsService,
    IReferralService referralService)
{
    public ILogger<ReferralServices> Logger { get; } = logger;
    public IIdentityService IdentityService { get; } = identityService;
    public IReferralSettingsService ReferralSettingsService { get; } = referralSettingsService;
    public IReferralService ReferralService { get; } = referralService;
}

public static class ReferralEndpoints
{
    public static void MapReferralEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("referrals").RequireAuthorization();

        group.MapGet("/settings", GetReferralSettings).WithName("GetReferralSettingsForUser");
        group.MapPost("/invites", CreateInvites).WithName("CreateReferralInvites");
        group.MapGet("/invites", ListInvites).WithName("ListReferralInvites");
        group.MapPost("/invites/{inviteId:guid}/resend", ResendInvite).WithName("ResendReferralInvite");
        group.MapPost("/accept", AcceptInvite).WithName("AcceptReferralInvite");

        // Public preview endpoint
        app.MapGet("/referrals/preview", PreviewInvite)
            .WithName("PreviewReferralInvite")
            .AllowAnonymous();
    }

    private static async Task<IResult> GetReferralSettings([AsParameters] ReferralServices services)
    {
        // User-only (not coaches)
        if (services.IdentityService.IsCoach)
        {
            return Results.Forbid();
        }

        try
        {
            var settings = await services.ReferralSettingsService.GetReferralSettingsAsync();
            return Results.Ok(new
            {
                enabled = settings.Enabled,
                maxRewardsPerInviter = settings.MaxRewardsPerInviter,
                inviterFreeMonths = settings.InviterFreeMonths,
                inviteeFreeMonths = settings.InviteeFreeMonths,
                inviteTokenExpiryDays = settings.InviteTokenExpiryDays,
                maxInvitesPerRequest = settings.MaxInvitesPerRequest,
                requireInviterActiveSubscriberToEarn = settings.RequireInviterActiveSubscriberToEarn
            });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving referral settings");
            return Results.Problem("An error occurred while retrieving referral settings");
        }
    }

    private static async Task<IResult> CreateInvites(
        [AsParameters] ReferralServices services,
        CreateReferralInvitesRequest request,
        HttpContext httpContext)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        // User-only (not coaches)
        if (services.IdentityService.IsCoach)
        {
            return Results.Forbid();
        }

        var user = await services.IdentityService.GetUserAsync();
        if (user == null)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return Results.BadRequest(new { error = "User email is required" });
        }

        try
        {
            var publicBaseUrl = GetPublicBaseUrlFromRequest(httpContext.Request);
            var result = await services.ReferralService.CreateInvitesAsync(
                inviterUserId: userId,
                inviterEmail: user.Email,
                inviterName: user.FullName,
                emails: request.Emails,
                baseUrl: publicBaseUrl);

            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error creating referral invites for user {UserId}", userId);
            return Results.Problem("An error occurred while creating referral invites");
        }
    }

    private static async Task<IResult> ListInvites([AsParameters] ReferralServices services)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        // User-only (not coaches)
        if (services.IdentityService.IsCoach)
        {
            return Results.Forbid();
        }

        try
        {
            var invites = await services.ReferralService.GetInvitesForInviterAsync(userId);
            return Results.Ok(invites);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error listing referral invites for user {UserId}", userId);
            return Results.Problem("An error occurred while retrieving referral invites");
        }
    }

    private static async Task<IResult> ResendInvite(
        [AsParameters] ReferralServices services,
        Guid inviteId,
        HttpContext httpContext)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        // User-only (not coaches)
        if (services.IdentityService.IsCoach)
        {
            return Results.Forbid();
        }

        var user = await services.IdentityService.GetUserAsync();
        if (user == null)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return Results.BadRequest(new { error = "User email is required" });
        }

        try
        {
            var publicBaseUrl = GetPublicBaseUrlFromRequest(httpContext.Request);
            var invite = await services.ReferralService.ResendInviteAsync(
                inviterUserId: userId,
                inviteId: inviteId,
                inviterEmail: user.Email,
                inviterName: user.FullName,
                baseUrl: publicBaseUrl);

            return Results.Ok(invite);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error resending referral invite {InviteId} for user {UserId}", inviteId, userId);
            return Results.Problem("An error occurred while resending the referral invite");
        }
    }

    private static async Task<IResult> PreviewInvite(
        [AsParameters] ReferralServices services,
        [FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Results.BadRequest(new { error = "Token is required" });
        }

        try
        {
            var preview = await services.ReferralService.PreviewInviteAsync(token);
            return Results.Ok(preview);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error previewing referral invite");
            return Results.Problem("An error occurred while previewing the referral invite");
        }
    }

    private static async Task<IResult> AcceptInvite(
        [AsParameters] ReferralServices services,
        AcceptReferralInviteRequest request)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        // User-only (not coaches)
        if (services.IdentityService.IsCoach)
        {
            return Results.Forbid();
        }

        var user = await services.IdentityService.GetUserAsync();
        if (user == null)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return Results.BadRequest(new { error = "User email is required to accept an invite" });
        }

        try
        {
            await services.ReferralService.AcceptInviteAsync(
                inviteeUserId: userId,
                inviteeEmail: user.Email,
                token: request.Token);

            return Results.Ok(new { message = "Invite accepted" });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error accepting referral invite for user {UserId}", userId);
            return Results.Problem("An error occurred while accepting the referral invite");
        }
    }

    private static string? GetPublicBaseUrlFromRequest(HttpRequest request)
    {
        // Passed from the frontend API route to ensure invite links point back at the correct app host.
        var raw = request.Headers["X-Public-Base-Url"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        // Only allow http(s) absolute base URLs (strip any path/query/fragment).
        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri))
        {
            return null;
        }

        if (!string.Equals(uri.Scheme, "http", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var builder = new UriBuilder(uri.Scheme, uri.Host, uri.Port);
        return builder.Uri.ToString().TrimEnd('/');
    }
}

public class CreateReferralInvitesRequest
{
    public required List<string> Emails { get; init; } = new();
}

public class AcceptReferralInviteRequest
{
    public required string Token { get; init; } = string.Empty;
}

