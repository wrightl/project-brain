using LaunchDarkly.Sdk;
using LaunchDarkly.Sdk.Server;
using LaunchDarkly.Sdk.Server.Interfaces;
using ProjectBrain.Api.Authentication;

public class FeatureFlagService
{
    private readonly IIdentityService _identityService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FeatureFlagService> _logger;

    // Inject the ILdClient singleton
    public FeatureFlagService(IIdentityService IdentityService, IConfiguration configuration, ILogger<FeatureFlagService> logger)
    {
        _logger = logger;
        _configuration = configuration;
        _identityService = IdentityService;
    }

    private LdClient getLdClient()
    {
        string SdkKey = _configuration["LaunchDarkly:SdkKey"] ?? throw new InvalidOperationException("LaunchDarkly SdkKey is not configured.");

        var ldConfig = Configuration.Default(SdkKey);
        var client = new LdClient(ldConfig);
        return client;
    }

    /// <summary>
    /// Evaluates a boolean feature flag for a given user.
    /// </summary>
    /// <param name="flagKey">The key of the flag in LaunchDarkly.</param>
    /// <param name="user">The ClaimsPrincipal representing the current user.</param>
    /// <param name="defaultValue">The value to return if LD is unreachable or an error occurs.</param>
    /// <returns>The evaluated flag value.</returns>
    public async Task<bool> GetBoolFlag(string flagKey, bool defaultValue = false)
    {
        try
        {
            // 1. Build the Context from the user's claims
            var context = await BuildContext();

            // 2. Evaluate the flag
            return getLdClient().BoolVariation(flagKey, context, defaultValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error evaluating flag '{flagKey}' for user '{_identityService.UserId}'. Returning default value.");
            return defaultValue;
        }
    }

    /// <summary>
    /// Evaluates a string feature flag for a given user.
    /// </summary>
    public async Task<string> GetStringFlag(string flagKey, string defaultValue = "")
    {
        try
        {
            var context = await BuildContext();
            return getLdClient().StringVariation(flagKey, context, defaultValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error evaluating flag '{flagKey}'. Returning default value.");
            return defaultValue;
        }
    }

    // Helper method to build the LaunchDarkly Context consistently
    private async Task<Context> BuildContext()
    {
        // If user is null or not authenticated, treat as anonymous
        if (!_identityService.IsAuthenticated)
        {
            return Context.Builder("anonymous")
                .Kind("user")
                .Anonymous(true)
                .Build();
        }

        var userId = _identityService.UserId;
        var userEmail = _identityService.UserEmail;

        // Read custom roles from the Auth0 token.
        // Ensure this claim type matches exactly what's in your token.
        var roles = (await _identityService.GetUserAsync())?.Roles;
        var ldRoles = roles?.Select(role => LdValue.Of(role)).ToArray();

        return Context.Builder(userId)
            .Kind("user")
            .Name(userEmail)
            .Set("email", userEmail)
            .Set("roles", LdValue.ArrayOf(ldRoles))
            .Build();
    }

    public async Task<bool> IsCoachSectionEnabled()
    {
        return await GetBoolFlag(FeatureFlags.EnableCoachSection, false);
    }
}

public static class FeatureFlags
{
    public const string EnableCoachSection = "enable-coach-section";
}

public static class FeatureFlagExtensions
{
    public static IServiceCollection AddFeatureFlags(this IServiceCollection services)
    {
        services.AddScoped<FeatureFlagService>();
        return services;
    }
}