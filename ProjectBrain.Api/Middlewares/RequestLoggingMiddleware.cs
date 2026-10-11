namespace ProjectBrain.Api.Middlewares;

public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    // Container Apps probes these every few seconds on an always-on replica; logging them
    // floods Log Analytics. Tracing already excludes them (ServiceDefaults).
    private static readonly PathString[] ExcludedPaths =
    [
        new("/alive"),
        new("/health"),
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsExcluded(context.Request.Path))
        {
            await next(context);
            return;
        }

        var correlationId = context.TraceIdentifier;
        var startedAt = DateTime.UtcNow;

        logger.LogInformation(
            "HTTP {Method} {Path} started (correlationId={CorrelationId})",
            context.Request.Method,
            context.Request.Path,
            correlationId);

        try
        {
            await next(context);
        }
        finally
        {
            var elapsedMs = (DateTime.UtcNow - startedAt).TotalMilliseconds;
            logger.LogInformation(
                "HTTP {Method} {Path} completed {StatusCode} in {ElapsedMs}ms (correlationId={CorrelationId})",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                elapsedMs,
                correlationId);
        }
    }

    private static bool IsExcluded(PathString path)
    {
        foreach (var excluded in ExcludedPaths)
        {
            if (path.StartsWithSegments(excluded))
            {
                return true;
            }
        }

        return false;
    }
}

public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app) =>
        app.UseMiddleware<RequestLoggingMiddleware>();
}
