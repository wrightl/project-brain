namespace ProjectBrain.AI;

using ProjectBrain.Domain;

public static class TokenEstimatorFactory
{
    [Obsolete("Use CreateAsync instead.")]
    public static ITokenEstimator Create(IApplicationSettingsService settingsService)
        => CreateAsync(settingsService).GetAwaiter().GetResult();

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
