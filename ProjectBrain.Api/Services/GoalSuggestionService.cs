using ProjectBrain.AI;
using ProjectBrain.Api.Goals;
using ProjectBrain.Domain;

namespace ProjectBrain.Api.Services;

public sealed class GoalSuggestionService : IGoalSuggestionService
{
    private readonly IGoalService _goalService;
    private readonly IGoalDailySuggestionClient _goalDailySuggestionClient;
    private readonly IGoalSuggestionUserContext _goalSuggestionUserContext;
    private readonly IUsageTrackingService _usageTrackingService;

    public GoalSuggestionService(
        IGoalService goalService,
        IGoalDailySuggestionClient goalDailySuggestionClient,
        IGoalSuggestionUserContext goalSuggestionUserContext,
        IUsageTrackingService usageTrackingService)
    {
        _goalService = goalService;
        _goalDailySuggestionClient = goalDailySuggestionClient;
        _goalSuggestionUserContext = goalSuggestionUserContext;
        _usageTrackingService = usageTrackingService;
    }

    public async Task<GoalSuggestionResult> SuggestDailyGoalsAsync(
        string userId,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var userInformation = await _goalSuggestionUserContext.LoadOnboardingMarkdownAsync(userId, cancellationToken);
        var backlog = await _goalService.GetPrioritizedIncompleteGoalBacklogAsync(userId, cancellationToken: cancellationToken);
        var todaysMessages = (await _goalService.GetTodaysGoalsAsync(userId, cancellationToken))
            .Select(g => g.Message.Trim())
            .Where(m => m.Length > 0)
            .ToList();

        var suggested = await _goalDailySuggestionClient.GetSuggestedDailyGoalsAsync(
            userId,
            userName,
            userInformation,
            backlog,
            todaysMessages,
            cancellationToken);

        await _usageTrackingService.TrackAIQueryAsync(userId);

        return new GoalSuggestionResult
        {
            Goals = suggested.ToList(),
            Source = backlog.Count > 0 ? "history" : "profile"
        };
    }
}
