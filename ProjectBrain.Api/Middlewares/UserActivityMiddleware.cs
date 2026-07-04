using ProjectBrain.Api.Authentication;
using ProjectBrain.Api.Background;
using ProjectBrain.Database;

namespace ProjectBrain.Api.Middlewares;

public class UserActivityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserActivityMiddleware> _logger;

    public UserActivityMiddleware(
        RequestDelegate next,
        ILogger<UserActivityMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IIdentityService identityService,
        IDatabaseStartupState startupState,
        IUserActivityQueue userActivityQueue)
    {
        if (startupState.IsWarmedUp
            && identityService.IsAuthenticated
            && !string.IsNullOrEmpty(identityService.UserId))
        {
            try
            {
                userActivityQueue.Enqueue(identityService.UserId!);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to enqueue user activity for user {UserId}", identityService.UserId);
            }
        }

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
