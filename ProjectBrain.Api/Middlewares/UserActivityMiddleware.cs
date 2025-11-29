using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain;

namespace ProjectBrain.Api.Middlewares;

public class UserActivityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserActivityMiddleware> _logger;

    public UserActivityMiddleware(RequestDelegate next, ILogger<UserActivityMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IUserActivityService userActivityService,
        IIdentityService identityService)
    {
        // Only track activity for authenticated users
        if (identityService.IsAuthenticated && !string.IsNullOrEmpty(identityService.UserId))
        {
            try
            {
                // Record activity - use background task queue pattern to avoid blocking
                // but ensure it completes even if request ends
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await userActivityService.RecordUserActivityAsync(identityService.UserId!);
                    }
                    catch (Exception ex)
                    {
                        // Log but don't throw - activity tracking shouldn't break requests
                        _logger.LogWarning(ex, "Failed to record user activity for user {UserId}", identityService.UserId);
                    }
                }, cancellationToken: context.RequestAborted);
            }
            catch (Exception ex)
            {
                // Log but don't throw - activity tracking shouldn't break requests
                _logger.LogWarning(ex, "Failed to initiate user activity tracking for user {UserId}", identityService.UserId);
            }
        }

        // Continue to next middleware
        await _next(context);
    }
}

public static class UserActivityMiddlewareExtensions
{
    public static IApplicationBuilder UseUserActivityTracking(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<UserActivityMiddleware>();
    }
}

