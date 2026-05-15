using ProjectBrain.Domain;
using ProjectBrain.Domain.UnitOfWork;

namespace ProjectBrain.Api.Background;

internal static class ChatPersistenceHelper
{
    public static async Task EnqueueOrPersistAsync(
        IChatPersistenceQueue chatPersistenceQueue,
        IChatService chatService,
        IUsageTrackingService usageTrackingService,
        IUnitOfWork unitOfWork,
        Guid conversationId,
        string userId,
        string userContent,
        string assistantContent,
        CancellationToken cancellationToken)
    {
        var dto = new ChatPersistenceQueueMessage
        {
            SchemaVersion = "1",
            ConversationId = conversationId,
            UserId = userId,
            UserContent = userContent,
            AssistantContent = assistantContent
        };

        if (await chatPersistenceQueue.TryEnqueueAsync(dto, cancellationToken).ConfigureAwait(false))
            return;

        await PersistSynchronouslyAsync(chatService, usageTrackingService, unitOfWork, conversationId, userId, userContent, assistantContent)
            .ConfigureAwait(false);
    }

    public static async Task PersistSynchronouslyAsync(
        IChatService chatService,
        IUsageTrackingService usageTrackingService,
        IUnitOfWork unitOfWork,
        Guid conversationId,
        string userId,
        string userContent,
        string assistantContent)
    {
        var transactionStarted = false;
        var committed = false;

        try
        {
            await unitOfWork.BeginTransactionAsync().ConfigureAwait(false);
            transactionStarted = true;

            var now = DateTime.UtcNow;
            await chatService.AddMany(
                new List<ChatMessage>
                {
                    new ChatMessage
                    {
                        ConversationId = conversationId,
                        Role = "user",
                        Content = userContent,
                        CreatedAt = now,
                        UpdatedAt = now,
                        Status = ""
                    },
                    new ChatMessage
                    {
                        ConversationId = conversationId,
                        Role = "assistant",
                        Content = assistantContent,
                        CreatedAt = now,
                        UpdatedAt = now,
                        Status = ""
                    }
                }).ConfigureAwait(false);

            await usageTrackingService.TrackAIQueryAsync(userId).ConfigureAwait(false);
            await unitOfWork.CommitTransactionAsync().ConfigureAwait(false);
            committed = true;
        }
        catch
        {
            if (transactionStarted && !committed)
            {
                try
                {
                    await unitOfWork.RollbackTransactionAsync().ConfigureAwait(false);
                }
                catch
                {
                    // CommitTransactionAsync may already have rolled back after a commit failure.
                }
            }

            throw;
        }
    }
}
