using Microsoft.AspNetCore.Mvc;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain;
using ProjectBrain.Shared.Dtos.Settings;

public class ApplicationSettingsServices(
    ILogger<ApplicationSettingsServices> logger,
    IIdentityService identityService,
    IApplicationSettingsService applicationSettingsService,
    ISubscriptionService subscriptionService)
{
    public ILogger<ApplicationSettingsServices> Logger { get; } = logger;
    public IIdentityService IdentityService { get; } = identityService;
    public IApplicationSettingsService ApplicationSettingsService { get; } = applicationSettingsService;
    public ISubscriptionService SubscriptionService { get; } = subscriptionService;
}

public static class ApplicationSettingsEndpoints
{
    private const string ReferralSettingsCategory = "Referral";

    private const string ReferralEnabledKey = "Referral:Enabled";
    private const string ReferralMaxRewardsPerInviterKey = "Referral:MaxRewardsPerInviter";
    private const string ReferralInviterFreeMonthsKey = "Referral:InviterFreeMonths";
    private const string ReferralInviteeFreeMonthsKey = "Referral:InviteeFreeMonths";
    private const string ReferralInviteTokenExpiryDaysKey = "Referral:InviteTokenExpiryDays";
    private const string ReferralMaxInvitesPerRequestKey = "Referral:MaxInvitesPerRequest";
    private const string ReferralRequireInviterActiveSubscriberToEarnKey = "Referral:RequireInviterActiveSubscriberToEarn";

    public static void MapApplicationSettingsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("admin/settings").RequireAuthorization("AdminOnly");

        // Settings endpoints
        group.MapGet("", GetAllSettings).WithName("GetAllSettings");
        group.MapGet("/category/{category}", GetSettingsByCategory).WithName("GetSettingsByCategory");
        group.MapGet("/ai", GetAISettings).WithName("GetAISettings");
        group.MapGet("/subscription", GetSubscriptionSettings).WithName("GetSubscriptionSettings");
        group.MapGet("/referrals", GetReferralSettings).WithName("GetReferralSettings");
        group.MapPut("/{key}", UpdateSetting).WithName("UpdateSetting");
        group.MapPut("/ai", UpdateAISettings).WithName("UpdateAISettings");
        group.MapPut("/subscription", UpdateSubscriptionSettings).WithName("UpdateSubscriptionSettings");
        group.MapPut("/referrals", UpdateReferralSettings).WithName("UpdateReferralSettings");
    }

    private static async Task<IResult> GetAllSettings([AsParameters] ApplicationSettingsServices services)
    {
        if (!services.IdentityService.IsAdmin)
        {
            return Results.Forbid();
        }

        try
        {
            var settings = await services.ApplicationSettingsService.GetAllSettingsAsync();
            var dtos = settings.Select(s => new ApplicationSettingDto
            {
                Key = s.Key,
                Value = s.Value,
                Category = s.Category,
                Description = s.Description,
                UpdatedAt = s.UpdatedAt.ToString("O"),
                UpdatedBy = s.UpdatedBy
            }).ToList();

            return Results.Ok(dtos);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving all settings");
            return Results.Problem("An error occurred while retrieving settings");
        }
    }

    private static async Task<IResult> GetSettingsByCategory(
        [AsParameters] ApplicationSettingsServices services,
        string category)
    {
        if (!services.IdentityService.IsAdmin)
        {
            return Results.Forbid();
        }

        try
        {
            var settings = await services.ApplicationSettingsService.GetSettingsByCategoryAsync(category);
            var dtos = settings.Select(s => new ApplicationSettingDto
            {
                Key = s.Key,
                Value = s.Value,
                Category = s.Category,
                Description = s.Description,
                UpdatedAt = s.UpdatedAt.ToString("O"),
                UpdatedBy = s.UpdatedBy
            }).ToList();

            return Results.Ok(dtos);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving settings for category {Category}", category);
            return Results.Problem("An error occurred while retrieving settings");
        }
    }

    private static async Task<IResult> GetAISettings([AsParameters] ApplicationSettingsServices services)
    {
        if (!services.IdentityService.IsAdmin)
        {
            return Results.Forbid();
        }

        try
        {
            var settings = await services.ApplicationSettingsService.GetAISettingsAsync();
            var dto = new AISettingsDto
            {
                MaxSearchResults = settings.MaxSearchResults,
                MaxContentLengthPerSource = settings.MaxContentLengthPerSource,
                MaxHistoryMessages = settings.MaxHistoryMessages,
                MaxTotalTokens = settings.MaxTotalTokens
            };

            return Results.Ok(dto);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving AI settings");
            return Results.Problem("An error occurred while retrieving AI settings");
        }
    }

    private static async Task<IResult> UpdateSetting(
        [AsParameters] ApplicationSettingsServices services,
        string key,
        UpdateApplicationSettingRequestDto request)
    {
        if (!services.IdentityService.IsAdmin)
        {
            return Results.Forbid();
        }

        var adminId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(adminId))
        {
            return Results.Unauthorized();
        }

        try
        {
            await services.ApplicationSettingsService.UpdateSettingAsync(key, request.Value, adminId);
            return Results.Ok(new { message = "Setting updated successfully" });
        }
        catch (InvalidOperationException ex)
        {
            services.Logger.LogWarning(ex, "Setting with key {Key} not found", key);
            return Results.NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error updating setting {Key}", key);
            return Results.Problem("An error occurred while updating the setting");
        }
    }

    private static async Task<IResult> UpdateAISettings(
        [AsParameters] ApplicationSettingsServices services,
        UpdateAISettingsRequestDto request)
    {
        if (!services.IdentityService.IsAdmin)
        {
            return Results.Forbid();
        }

        var adminId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(adminId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var settings = new AISettings
            {
                MaxSearchResults = request.MaxSearchResults,
                MaxContentLengthPerSource = request.MaxContentLengthPerSource,
                MaxHistoryMessages = request.MaxHistoryMessages,
                MaxTotalTokens = request.MaxTotalTokens
            };

            await services.ApplicationSettingsService.UpdateAISettingsAsync(settings, adminId);
            return Results.Ok(new { message = "AI settings updated successfully" });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error updating AI settings");
            return Results.Problem("An error occurred while updating AI settings");
        }
    }

    private static async Task<IResult> GetSubscriptionSettings([AsParameters] ApplicationSettingsServices services)
    {
        if (!services.IdentityService.IsAdmin)
        {
            return Results.Forbid();
        }

        try
        {
            var settings = await services.SubscriptionService.GetSubscriptionSettingsAsync();
            var dto = new SubscriptionSettingsDto
            {
                EnableUserSubscriptions = settings.EnableUserSubscriptions,
                EnableCoachSubscriptions = settings.EnableCoachSubscriptions
            };

            return Results.Ok(dto);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving subscription settings");
            return Results.Problem("An error occurred while retrieving subscription settings");
        }
    }

    private static async Task<IResult> UpdateSubscriptionSettings(
        [AsParameters] ApplicationSettingsServices services,
        UpdateSubscriptionSettingsRequestDto request)
    {
        if (!services.IdentityService.IsAdmin)
        {
            return Results.Forbid();
        }

        var adminId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(adminId))
        {
            return Results.Unauthorized();
        }

        try
        {
            await services.SubscriptionService.UpdateSubscriptionSettingsAsync(
                request.EnableUserSubscriptions,
                request.EnableCoachSubscriptions,
                adminId);

            return Results.Ok(new { message = "Subscription settings updated successfully" });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error updating subscription settings");
            return Results.Problem("An error occurred while updating subscription settings");
        }
    }

    private static async Task<IResult> GetReferralSettings([AsParameters] ApplicationSettingsServices services)
    {
        if (!services.IdentityService.IsAdmin)
        {
            return Results.Forbid();
        }

        try
        {
            var enabledRaw = await services.ApplicationSettingsService.GetSettingAsync(ReferralEnabledKey);
            var maxRewardsRaw = await services.ApplicationSettingsService.GetSettingAsync(ReferralMaxRewardsPerInviterKey);
            var inviterMonthsRaw = await services.ApplicationSettingsService.GetSettingAsync(ReferralInviterFreeMonthsKey);
            var inviteeMonthsRaw = await services.ApplicationSettingsService.GetSettingAsync(ReferralInviteeFreeMonthsKey);
            var expiryDaysRaw = await services.ApplicationSettingsService.GetSettingAsync(ReferralInviteTokenExpiryDaysKey);
            var maxInvitesRaw = await services.ApplicationSettingsService.GetSettingAsync(ReferralMaxInvitesPerRequestKey);
            var requireInviterPaidRaw = await services.ApplicationSettingsService.GetSettingAsync(ReferralRequireInviterActiveSubscriberToEarnKey);

            var dto = new ReferralSettingsDto
            {
                Enabled = bool.TryParse(enabledRaw, out var enabled) ? enabled : false,
                MaxRewardsPerInviter = int.TryParse(maxRewardsRaw, out var maxRewards) ? maxRewards : 12,
                InviterFreeMonths = int.TryParse(inviterMonthsRaw, out var inviterMonths) ? inviterMonths : 1,
                InviteeFreeMonths = int.TryParse(inviteeMonthsRaw, out var inviteeMonths) ? inviteeMonths : 1,
                InviteTokenExpiryDays = int.TryParse(expiryDaysRaw, out var expiryDays) ? expiryDays : 30,
                MaxInvitesPerRequest = int.TryParse(maxInvitesRaw, out var maxInvites) ? maxInvites : 10,
                RequireInviterActiveSubscriberToEarn =
                    bool.TryParse(requireInviterPaidRaw, out var requireInviterPaid) ? requireInviterPaid : false
            };

            return Results.Ok(dto);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving referral settings");
            return Results.Problem("An error occurred while retrieving referral settings");
        }
    }

    private static async Task<IResult> UpdateReferralSettings(
        [AsParameters] ApplicationSettingsServices services,
        UpdateReferralSettingsRequestDto request)
    {
        if (!services.IdentityService.IsAdmin)
        {
            return Results.Forbid();
        }

        var adminId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(adminId))
        {
            return Results.Unauthorized();
        }

        // Basic validation (keep server-side enforcement)
        if (request.MaxRewardsPerInviter < 0 ||
            request.InviterFreeMonths < 0 ||
            request.InviteeFreeMonths < 0 ||
            request.InviteTokenExpiryDays < 1 ||
            request.MaxInvitesPerRequest < 1 ||
            request.MaxInvitesPerRequest > 10)
        {
            return Results.BadRequest(new { error = "Invalid referral settings values" });
        }

        try
        {
            // These keys are seeded by ProjectBrainDbInitializer. We intentionally do not create them here.
            await services.ApplicationSettingsService.UpdateSettingAsync(
                ReferralEnabledKey,
                request.Enabled.ToString().ToLowerInvariant(),
                adminId);

            await services.ApplicationSettingsService.UpdateSettingAsync(
                ReferralMaxRewardsPerInviterKey,
                request.MaxRewardsPerInviter.ToString(),
                adminId);

            await services.ApplicationSettingsService.UpdateSettingAsync(
                ReferralInviterFreeMonthsKey,
                request.InviterFreeMonths.ToString(),
                adminId);

            await services.ApplicationSettingsService.UpdateSettingAsync(
                ReferralInviteeFreeMonthsKey,
                request.InviteeFreeMonths.ToString(),
                adminId);

            await services.ApplicationSettingsService.UpdateSettingAsync(
                ReferralInviteTokenExpiryDaysKey,
                request.InviteTokenExpiryDays.ToString(),
                adminId);

            await services.ApplicationSettingsService.UpdateSettingAsync(
                ReferralMaxInvitesPerRequestKey,
                request.MaxInvitesPerRequest.ToString(),
                adminId);

            await services.ApplicationSettingsService.UpdateSettingAsync(
                ReferralRequireInviterActiveSubscriberToEarnKey,
                request.RequireInviterActiveSubscriberToEarn.ToString().ToLowerInvariant(),
                adminId);

            return Results.Ok(new { message = "Referral settings updated successfully" });
        }
        catch (InvalidOperationException ex)
        {
            services.Logger.LogWarning(ex, "Referral settings key missing - seed required");
            return Results.NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error updating referral settings");
            return Results.Problem("An error occurred while updating referral settings");
        }
    }
}
