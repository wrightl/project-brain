using ProjectBrain.Domain;

namespace ProjectBrain.AI;

public interface IGoalDailySuggestionClient
{
    Task<IReadOnlyList<string>> GetSuggestedDailyGoalsAsync(
        string userId,
        string userName,
        string userInformation,
        IReadOnlyList<IncompleteGoalBacklogItem> backlog,
        IReadOnlyList<string> todaysExistingGoalMessages,
        CancellationToken cancellationToken = default);
}

public sealed class GoalDailySuggestionClient(AzureOpenAI azureOpenAI) : IGoalDailySuggestionClient
{
    public Task<IReadOnlyList<string>> GetSuggestedDailyGoalsAsync(
        string userId,
        string userName,
        string userInformation,
        IReadOnlyList<IncompleteGoalBacklogItem> backlog,
        IReadOnlyList<string> todaysExistingGoalMessages,
        CancellationToken cancellationToken = default) =>
        azureOpenAI.GetSuggestedDailyGoalsAsync(
            userId,
            userName,
            userInformation,
            backlog,
            todaysExistingGoalMessages,
            cancellationToken);
}
