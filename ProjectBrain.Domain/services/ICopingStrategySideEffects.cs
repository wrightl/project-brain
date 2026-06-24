namespace ProjectBrain.Domain;

public interface ICopingStrategySideEffects
{
    Task OnStrategyCreatedAsync(
        string userId,
        Guid strategyId,
        string title,
        string description,
        string? iconKey,
        int? rating,
        DateTime savedAt,
        Guid? conversationId,
        CancellationToken cancellationToken = default);

    Task EnqueueStrategyReindexAsync(
        string userId,
        Guid strategyId,
        string title,
        string description,
        string? iconKey,
        int? rating,
        DateTime savedAt,
        CancellationToken cancellationToken = default);
}
