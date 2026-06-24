using ProjectBrain.Api.Background;
using ProjectBrain.Domain;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces.Managers;

namespace ProjectBrain.Api.Services;

public sealed class GoalMutationSideEffectsService : IGoalMutationSideEffects
{
    private readonly IGoalsUpdatedBroadcaster _goalsUpdatedBroadcaster;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly ITimeTickerManager<TimeTickerEntity> _timeTickerManager;
    private readonly ILogger<GoalMutationSideEffectsService> _logger;

    public GoalMutationSideEffectsService(
        IGoalsUpdatedBroadcaster goalsUpdatedBroadcaster,
        IPushNotificationService pushNotificationService,
        ITimeTickerManager<TimeTickerEntity> timeTickerManager,
        ILogger<GoalMutationSideEffectsService> logger)
    {
        _goalsUpdatedBroadcaster = goalsUpdatedBroadcaster;
        _pushNotificationService = pushNotificationService;
        _timeTickerManager = timeTickerManager;
        _logger = logger;
    }

    public async Task NotifyGoalsChangedAsync(string userId, CancellationToken cancellationToken = default)
    {
        var evt = new GoalsUpdatedEvent { UpdatedAt = DateTime.UtcNow.ToString("O") };
        _goalsUpdatedBroadcaster.NotifyGoalsUpdated(userId, evt);

        _ = Task.Run(async () =>
        {
            try
            {
                await _pushNotificationService.SendDataOnlyToUserAsync(
                    userId,
                    new Dictionary<string, string> { ["type"] = "goals_updated" });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send goals_updated FCM to user {UserId}", userId);
            }
        }, CancellationToken.None);

        await UserContextTickerEnqueue.EnqueueGoalsUploadAsync(_timeTickerManager, userId);
    }
}
