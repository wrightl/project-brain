namespace ProjectBrain.AI;

using ProjectBrain.Domain;

public static class TokenEstimatorFactory
{
    public static ITokenEstimator Create(IApplicationSettingsService settingsService)
    {
        var promptBudget = settingsService.GetPromptBudgetSettingsAsync().GetAwaiter().GetResult();
        return Create(promptBudget.TokenEstimator);
    }

    public static async Task<ITokenEstimator> CreateAsync(IApplicationSettingsService settingsService)
    {
        var promptBudget = await settingsService.GetPromptBudgetSettingsAsync().ConfigureAwait(false);
        return Create(promptBudget.TokenEstimator);
    }

    public static ITokenEstimator Create(string? tokenEstimator)
    {
        if (string.Equals(tokenEstimator, "tiktoken", StringComparison.OrdinalIgnoreCase))
        {
            return new TiktokenEstimator();
        }

        return new CharacterTokenEstimator();
    }
}
