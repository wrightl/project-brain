namespace ProjectBrain.Api.Background;

public interface IChatPersistenceQueue
{
    Task<bool> TryEnqueueAsync(ChatPersistenceQueueMessage message, CancellationToken cancellationToken = default);
}
