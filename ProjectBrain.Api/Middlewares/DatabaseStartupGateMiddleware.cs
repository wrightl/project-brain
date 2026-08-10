using ProjectBrain.Database;

namespace ProjectBrain.Api.Middlewares;

/// <summary>
/// Returns 503 until SQL warmup completes so clients can retry during serverless resume
/// without ACA readiness probes needing to hit the database.
/// </summary>
public class DatabaseStartupGateMiddleware
{
    private static readonly PathString[] ExemptPaths =
    [
        new("/alive"),
        new("/health"),
    ];

    private readonly RequestDelegate _next;

    public DatabaseStartupGateMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IDatabaseStartupState startupState)
    {
        if (startupState.IsWarmedUp || IsExempt(context.Request.Path))
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.Headers.RetryAfter = "5";
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            error = "starting",
            message = "Service is starting up; retry shortly."
        });
    }

    private static bool IsExempt(PathString path)
    {
        foreach (var exempt in ExemptPaths)
        {
            if (path.StartsWithSegments(exempt))
            {
                return true;
            }
        }

        return false;
    }
}

public static class DatabaseStartupGateMiddlewareExtensions
{
    public static IApplicationBuilder UseDatabaseStartupGate(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<DatabaseStartupGateMiddleware>();
    }
}
