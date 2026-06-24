using ProjectBrain.Domain;

public class FeatureFlagServices(
    ILogger<FeatureFlagServices> logger,
    IFeatureFlagService featureService,
    IEmailService emailService)
{
    public ILogger<FeatureFlagServices> Logger { get; } = logger;
    public IFeatureFlagService FeatureService { get; } = featureService;
    public IEmailService EmailService { get; } = emailService;
}

public static class FeatureFlagEndpoints
{
    public static void MapFeatureFlagEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("feature-flags").RequireAuthorization();

        group.MapGet("/", GetAllFeatureFlags).WithName("GetAllFeatureFlags");
        group.MapGet("/{flagKey}", GetFeatureFlag).WithName("GetFeatureFlag");

    }

    private static async Task<IResult> GetAllFeatureFlags([AsParameters] FeatureFlagServices services)
    {
        try
        {
            var flags = await services.FeatureService.GetAllFlagsAsync();
            return Results.Ok(flags);
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error retrieving feature flags");
            return Results.Problem("An error occurred while retrieving feature flags.");
        }
    }

    private static async Task<IResult> GetFeatureFlag([AsParameters] FeatureFlagServices services, string flagKey)
    {
        var isEnabled = await services.FeatureService.IsFeatureEnabled(flagKey);
        return Results.Ok(isEnabled);
    }
}
