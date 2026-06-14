using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain;

public class PushNotificationServices(
    ILogger<PushNotificationServices> logger,
    IIdentityService identityService,
    IDeviceTokenService deviceTokenService,
    IPushNotificationService pushNotificationService)
{
    public ILogger<PushNotificationServices> Logger { get; } = logger;
    public IIdentityService IdentityService { get; } = identityService;
    public IDeviceTokenService DeviceTokenService { get; } = deviceTokenService;
    public IPushNotificationService PushNotificationService { get; } = pushNotificationService;
}

public static class PushNotificationEndpoints
{
    public static void MapPushNotificationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("push-notifications")
            .RequireAuthorization()
            .WithTags("Push Notifications");

        // Register/Update device token
        group.MapPost("/register-token", RegisterDeviceToken)
            .WithName("RegisterDeviceToken")
            .Produces(200)
            .Produces(401);

        // Send test notification
        group.MapPost("/test", SendTestNotification)
            .WithName("SendTestNotification")
            .Produces(200)
            .Produces(401)
            .Produces(404);

        // Remove device token
        group.MapDelete("/remove-token", RemoveDeviceToken)
            .WithName("RemoveDeviceToken")
            .Produces(200)
            .Produces(401)
            .Produces(404);
    }

    private static async Task<IResult> RegisterDeviceToken(
        [AsParameters] PushNotificationServices services,
        [FromBody] RegisterTokenRequest request)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var result = await services.DeviceTokenService.RegisterOrUpdateAsync(
                userId,
                request.Token,
                request.Platform,
                request.DeviceId);

            if (!result.Succeeded)
            {
                services.Logger.LogWarning(
                    "Rejected device token registration: token already registered to another user");
                return Results.Conflict(result.ErrorMessage);
            }

            services.Logger.LogInformation("Device token registered/updated for user {UserId}", userId);
            return Results.Ok();
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error registering device token for user {UserId}", userId);
            return Results.Problem("Failed to register device token");
        }
    }

    private static async Task<IResult> SendTestNotification(
        [AsParameters] PushNotificationServices services,
        [FromBody] SendNotificationRequest request)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var deviceTokens = await services.DeviceTokenService.GetActiveTokenStringsForUserAsync(userId);

            if (!deviceTokens.Any())
            {
                return Results.NotFound("No device tokens found for user");
            }

            var result = await services.PushNotificationService.SendNotificationToMultipleAsync(
                deviceTokens.ToList(),
                request.Title,
                request.Body,
                request.Data
            );

            if (result.Success || result.SuccessCount > 0)
            {
                services.Logger.LogInformation(
                    "Test notification sent to {SuccessCount}/{TotalCount} devices for user {UserId}",
                    result.SuccessCount, deviceTokens.Count, userId);
                return Results.Ok(new
                {
                    success = result.Success,
                    successCount = result.SuccessCount,
                    failureCount = result.FailureCount,
                    invalidTokens = result.InvalidTokens,
                    failedTokens = result.FailedTokens
                });
            }

            return Results.Problem("Failed to send test notification");
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error sending test notification for user {UserId}", userId);
            return Results.Problem("Failed to send test notification");
        }
    }

    private static async Task<IResult> RemoveDeviceToken(
        [AsParameters] PushNotificationServices services,
        [FromQuery] string token)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrEmpty(token))
        {
            return Results.BadRequest("Token is required");
        }

        try
        {
            var removed = await services.DeviceTokenService.DeactivateTokenForUserAsync(userId, token);
            if (!removed)
            {
                return Results.NotFound("Device token not found");
            }

            services.Logger.LogInformation("Device token removed for user {UserId}", userId);
            return Results.Ok();
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error removing device token for user {UserId}", userId);
            return Results.Problem("Failed to remove device token");
        }
    }

    public record RegisterTokenRequest(string Token, string? Platform, string? DeviceId);
    public record SendNotificationRequest(string Title, string Body, Dictionary<string, string>? Data = null);
}
