namespace ProjectBrain.Domain;

public interface IGoalMutationSideEffects
{
    Task NotifyGoalsChangedAsync(string userId, CancellationToken cancellationToken = default);
}
