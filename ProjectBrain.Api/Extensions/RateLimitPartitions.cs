namespace ProjectBrain.Api;

public static class RateLimitPartitions
{
    public const string AiPolicy = "ai";

    /// <summary>
    /// Partitions by authenticated user id, falling back to the client address for anonymous requests
    /// (webhooks, health probes). Requires UseForwardedHeaders so the address is the real client.
    /// </summary>
    public static string GetClientKey(HttpContext httpContext)
    {
        var userId = httpContext.User.GetUserId();
        if (!string.IsNullOrEmpty(userId))
        {
            return $"user:{userId}";
        }

        return $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }
}
