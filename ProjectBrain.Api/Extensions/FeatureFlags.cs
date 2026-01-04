// using LaunchDarkly.Sdk;
// using LaunchDarkly.Sdk.Server;
using Microsoft.FeatureManagement;



// public class LaunchDarklyFeatureFlagService : IFeatureFlagService
// {
//     private readonly IIdentityService _identityService;
//     private readonly IConfiguration _configuration;
//     private readonly ILogger<LaunchDarklyFeatureFlagService> _logger;

//     // Inject the ILdClient singleton
//     public LaunchDarklyFeatureFlagService(IIdentityService IdentityService, IConfiguration configuration, ILogger<LaunchDarklyFeatureFlagService> logger)
//     {
//         _logger = logger;
//         _configuration = configuration;
//         _identityService = IdentityService;
//     }

//     private LdClient getLdClient()
//     {
//         string SdkKey = _configuration["LaunchDarkly:SdkKey"] ?? throw new InvalidOperationException("LaunchDarkly SdkKey is not configured.");

//         var ldConfig = Configuration.Default(SdkKey);
//         var client = new LdClient(ldConfig);
//         return client;
//     }

//     /// <summary>
//     /// Evaluates a boolean feature flag for a given user.
//     /// </summary>
//     /// <param name="flagKey">The key of the flag in LaunchDarkly.</param>
//     /// <param name="user">The ClaimsPrincipal representing the current user.</param>
//     /// <param name="defaultValue">The value to return if LD is unreachable or an error occurs.</param>
//     /// <returns>The evaluated flag value.</returns>
//     public async Task<bool> GetBoolFlag(string flagKey, bool defaultValue = false)
//     {
//         try
//         {
//             // 1. Build the Context from the user's claims
//             var context = await BuildContext();

//             // 2. Evaluate the flag
//             return getLdClient().BoolVariation(flagKey, context, defaultValue);
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, $"Error evaluating flag '{flagKey}' for user '{_identityService.UserId}'. Returning default value.");
//             return defaultValue;
//         }
//     }

//     /// <summary>
//     /// Evaluates a string feature flag for a given user.
//     /// </summary>
//     public async Task<string> GetStringFlag(string flagKey, string defaultValue = "")
//     {
//         try
//         {
//             var context = await BuildContext();
//             return getLdClient().StringVariation(flagKey, context, defaultValue);
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, $"Error evaluating flag '{flagKey}'. Returning default value.");
//             return defaultValue;
//         }
//     }

//     // Helper method to build the LaunchDarkly Context consistently
//     private async Task<Context> BuildContext()
//     {
//         // If user is null or not authenticated, treat as anonymous
//         if (!_identityService.IsAuthenticated)
//         {
//             return Context.Builder("anonymous")
//                 .Kind("user")
//                 .Anonymous(true)
//                 .Build();
//         }

//         var userId = _identityService.UserId;
//         var userEmail = _identityService.UserEmail;

//         // Read custom roles from the Auth0 token.
//         // Ensure this claim type matches exactly what's in your token.
//         var roles = (await _identityService.GetUserAsync())?.Roles;
//         var ldRoles = roles?.Select(role => LdValue.Of(role)).ToArray();

//         return Context.Builder(userId)
//             .Kind("user")
//             .Name(userEmail)
//             .Set("email", userEmail)
//             .Set("roles", LdValue.ArrayOf(ldRoles))
//             .Build();
//     }

//     public async Task<bool> IsCoachSectionEnabled()
//     {
//         return await GetBoolFlag(FeatureFlags.EnableCoachSection, false);
//     }
// }

// /// <summary>
// /// Service for checking system-level feature flags that don't require user context.
// /// Useful for background jobs, email sending, and other system operations.
// /// </summary>
// public interface ISystemFeatureFlagService
// {
//     /// <summary>
//     /// Checks if a feature flag is enabled using a system/anonymous context.
//     /// </summary>
//     /// <param name="flagKey">The key of the feature flag</param>
//     /// <param name="defaultValue">The default value if the flag cannot be evaluated</param>
//     /// <returns>True if the feature flag is enabled, false otherwise</returns>
//     Task<bool> IsEnabledAsync(string flagKey, bool defaultValue = false);
// }

// /// <summary>
// /// Implementation of ISystemFeatureFlagService for system-level feature flags.
// /// Uses LaunchDarkly with an anonymous/system context (no user required).
// /// </summary>
// public class SystemFeatureFlagService : ISystemFeatureFlagService
// {
//     private readonly IConfiguration _configuration;
//     private readonly ILogger<SystemFeatureFlagService> _logger;
//     private LdClient? _ldClient;

//     public SystemFeatureFlagService(
//         IConfiguration configuration,
//         ILogger<SystemFeatureFlagService> logger)
//     {
//         _configuration = configuration;
//         _logger = logger;
//     }

//     private LdClient GetLdClient()
//     {
//         if (_ldClient != null)
//         {
//             return _ldClient;
//         }

//         string sdkKey = _configuration["LaunchDarkly:SdkKey"]
//             ?? throw new InvalidOperationException("LaunchDarkly SdkKey is not configured.");

//         var ldConfig = Configuration.Default(sdkKey);
//         _ldClient = new LdClient(ldConfig);
//         return _ldClient;
//     }

//     public async Task<bool> IsEnabledAsync(string flagKey, bool defaultValue = false)
//     {
//         try
//         {
//             // Use an anonymous system context for feature flag evaluation
//             var context = Context.Builder("system")
//                 .Kind("system")
//                 .Anonymous(false)
//                 .Build();

//             var client = GetLdClient();
//             var result = client.BoolVariation(flagKey, context, defaultValue);

//             return result;
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Error evaluating system feature flag '{FlagKey}'. Returning default value {DefaultValue}.", flagKey, defaultValue);
//             return defaultValue;
//         }
//     }
// }

public static class FeatureFlagExtensions
{
    public static WebApplicationBuilder AddFeatureFlags(this WebApplicationBuilder builder)
    {
        string env = builder.Environment.EnvironmentName;

        // Check if Azure App Configuration connection details are available
        // This prevents errors when running EF migrations or other design-time operations
        var connectionString = builder.Configuration.GetConnectionString("config");
        var endpoint = builder.Configuration["Aspire:Microsoft:Extensions:Configuration:AzureAppConfiguration:Endpoint"];
        var configConnectionString = builder.Configuration["Aspire:Microsoft:Extensions:Configuration:AzureAppConfiguration:ConnectionString"];

        if (!string.IsNullOrEmpty(connectionString) || !string.IsNullOrEmpty(endpoint) || !string.IsNullOrEmpty(configConnectionString))
        {
            builder.AddAzureAppConfiguration(
                "config",
                configureOptions: options =>
                {
                    options.Select("*");
                    options.Select("*", env);

                    // Configure refresh options
                    options.ConfigureRefresh(refresh =>
                    {
                        refresh.RegisterAll()
                            .SetRefreshInterval(TimeSpan.FromSeconds(30));
                        // refresh.Register("Sentinel", refreshAll: true)
                        //        .SetRefreshInterval(TimeSpan.FromSeconds(5));
                    });
                });
        }

        builder.Services.AddFeatureManagement();

        builder.Services.AddScoped<IFeatureFlagService, AzureAppConfigFeatureFlagService>();

        // LaunchDarkly is not used for now
        // builder.Services.AddScoped<IFeatureFlagService, LaunchDarklyFeatureFlagService>();
        // builder.Services.AddScoped<ISystemFeatureFlagService, SystemFeatureFlagService>();
        return builder;
    }
}