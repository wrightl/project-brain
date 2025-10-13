public static class RobotMiddleware
{
    public static void UseRobotMiddleware(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            // Check if the request is from a robot
            if (context.Request.Headers.ContainsKey("User-Agent") &&
                context.Request.Headers["User-Agent"].ToString().Contains("robot", StringComparison.OrdinalIgnoreCase))
            {
                // Log the robot request
                app.Logger.LogInformation("Robot detected: {UserAgent}", context.Request.Headers["User-Agent"]);

                // Optionally, you can return a specific response for robots
                context.Response.StatusCode = 403; // Forbidden
                await context.Response.WriteAsync("Access denied for robots.");
                return;
            }

            // Proceed to the next middleware if not a robot
            await next.Invoke();
        });
    }
}