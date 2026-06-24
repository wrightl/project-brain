namespace ProjectBrain.Domain.AgentTools;

using ProjectBrain.Domain;

public static class AgentToolGating
{
    public static async Task<bool> IsCoachFeatureEnabledAsync(AgentToolContext context, CancellationToken cancellationToken)
    {
        if (context.FeatureFlagService is null)
        {
            return true;
        }

        return await context.FeatureFlagService.IsFeatureEnabled(FeatureFlags.EnableCoachSection);
    }

    public static async Task<bool> IsFileUploadEnabledAsync(AgentToolContext context, CancellationToken cancellationToken)
    {
        if (context.FeatureGateService is null)
        {
            return true;
        }

        var (allowed, _) = await context.FeatureGateService.CheckFeatureAccessAsync(
            context.UserId,
            context.UserType,
            "file_upload");
        return allowed;
    }
}
