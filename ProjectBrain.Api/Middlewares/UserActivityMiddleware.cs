using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace ProjectBrain.Api.Middlewares;

public class UserActivityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserActivityMiddleware> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public UserActivityMiddleware(
        RequestDelegate next,
        ILogger<UserActivityMiddleware> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _next = next;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IIdentityService identityService)
    {
        // Only track activity for authenticated users
        if (identityService.IsAuthenticated && !string.IsNullOrEmpty(identityService.UserId))
        {
            // Capture userId before Task.Run to avoid closure issues
            var userId = identityService.UserId!;

            try
            {
                // Record activity - use background task queue pattern to avoid blocking
                // Create a new scope for the background task to avoid DbContext threading issues
                _ = Task.Run(async () =>
                {
                    // Create a new scope for this background task
                    using var scope = _serviceScopeFactory.CreateScope();
                    var userActivityService = scope.ServiceProvider.GetRequiredService<IUserActivityService>();

                    try
                    {
                        await userActivityService.RecordUserActivityAsync(userId);
                    }
                    catch (Exception ex)
                    {
                        // Log but don't throw - activity tracking shouldn't break requests
                        _logger.LogWarning(ex, "Failed to record user activity for user {UserId}", userId);
                    }
                }, cancellationToken: context.RequestAborted);
            }
            catch (Exception ex)
            {
                // Log but don't throw - activity tracking shouldn't break requests
                _logger.LogWarning(ex, "Failed to initiate user activity tracking for user {UserId}", userId);
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

