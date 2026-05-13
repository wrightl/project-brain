namespace ProjectBrain.Api.Background;

/// <summary>Used when no Azure Storage Queues connection is configured; callers should fall back to synchronous persistence.</summary>
public sealed class NullChatPersistenceQueue : IChatPersistenceQueue
{
    public Task<bool> TryEnqueueAsync(ChatPersistenceQueueMessage message, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
}
