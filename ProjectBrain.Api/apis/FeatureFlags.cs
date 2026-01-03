using System.Reflection;
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
        group.MapGet("/test", SendEmail).WithName("SendEmailTest").AllowAnonymous();

    }

    private static async Task<IResult> SendEmail([AsParameters] FeatureFlagServices services)
    {
        await services.EmailService.SendEmailAsync("leewright76@hotmail.com", subject: "Test Email", htmlBody: "This is a test email");
        return Results.Ok("Email sent");
    }

    private static async Task<IResult> GetAllFeatureFlags([AsParameters] FeatureFlagServices services)
    {
        try
        {
            // Use reflection to get all public const string fields from the FeatureFlags class
            var featureFlagsType = typeof(FeatureFlags);
            var fields = featureFlagsType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
                .ToList();

            var flags = new Dictionary<string, bool>();

            // Check each flag's status
            foreach (var field in fields)
            {
                var flagKey = field.GetValue(null)?.ToString();
                if (string.IsNullOrEmpty(flagKey))
                {
                    continue;
                }

                try
                {
                    var isEnabled = await services.FeatureService.IsFeatureEnabled(flagKey);
                    flags[flagKey] = isEnabled;
                }
                catch (Exception ex)
                {
                    services.Logger.LogWarning(ex, "Error evaluating feature flag '{FlagKey}'. Skipping.", flagKey);
                    // Optionally include failed flags with a default value or skip them
                }
            }

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

