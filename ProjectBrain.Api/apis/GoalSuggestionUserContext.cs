namespace ProjectBrain.Api.Goals;

/// <summary>
/// Loads onboarding markdown used as profile context for AI goal suggestions.
/// </summary>
public interface IGoalSuggestionUserContext
{
    Task<string> LoadOnboardingMarkdownAsync(string userId, CancellationToken cancellationToken = default);
}

public sealed class StorageGoalSuggestionUserContext(Storage storage) : IGoalSuggestionUserContext
{
    public async Task<string> LoadOnboardingMarkdownAsync(string userId, CancellationToken cancellationToken = default)
    {
        var options = new StorageOptions
        {
            UserId = userId,
            FileOwnership = FileOwnership.User,
            StorageType = StorageType.Onboarding
        };

        var stream = await storage.GetFile(Constants.ONBOARDING_MARKDOWN_FILENAME, options);
        if (stream is null)
        {
            return string.Empty;
        }

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
