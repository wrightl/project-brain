using Microsoft.AspNetCore.Mvc;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain;
using ProjectBrain.Shared.Dtos.Settings;

public class ApplicationSettingsServices(
    ILogger<ApplicationSettingsServices> logger,
    IIdentityService identityService,
    IApplicationSettingsService applicationSettingsService)
{
    public ILogger<ApplicationSettingsServices> Logger { get; } = logger;
    public IIdentityService IdentityService { get; } = identityService;
    public IApplicationSettingsService ApplicationSettingsService { get; } = applicationSettingsService;
}

public static class ApplicationSettingsEndpoints
{
    public static void MapApplicationSettingsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("admin/settings").RequireAuthorization("AdminOnly");

        // Settings endpoints
        group.MapGet("", GetAllSettings).WithName("GetAllSettings");
        group.MapGet("/category/{category}", GetSettingsByCategory).WithName("GetSettingsByCategory");
        group.MapGet("/ai", GetAISettings).WithName("GetAISettings");
        group.MapPut("/{key}", UpdateSetting).WithName("UpdateSetting");
        group.MapPut("/ai", UpdateAISettings).WithName("UpdateAISettings");
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
}
