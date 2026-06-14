namespace ProjectBrain.Api.Middlewares;

public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
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
}

public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app) =>
        app.UseMiddleware<RequestLoggingMiddleware>();
}
