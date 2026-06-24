using ProjectBrain.Domain;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces.Managers;

namespace ProjectBrain.Api.Background;

internal static class ChatPersistenceHelper
{
    public static async Task EnqueueOrPersistAsync(
        IChatPersistenceQueue chatPersistenceQueue,
        IChatService chatService,
        IUsageTrackingService usageTrackingService,
        ITimeTickerManager<TimeTickerEntity>? timeTickerManager,
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

        await PersistSynchronouslyAsync(
                chatService,
                usageTrackingService,
                timeTickerManager,
                conversationId,
                userId,
                userContent,
                assistantContent)
            .ConfigureAwait(false);
    }

    public static async Task PersistSynchronouslyAsync(
        IChatService chatService,
        IUsageTrackingService usageTrackingService,
        ITimeTickerManager<TimeTickerEntity>? timeTickerManager,
        Guid conversationId,
        string userId,
        string userContent,
        string assistantContent)
    {
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

        if (timeTickerManager is not null)
        {
            try
            {
                await UserContextTickerEnqueue.EnqueueConversationContextSummaryAsync(
                    timeTickerManager,
                    userId,
                    conversationId,
                    CancellationToken.None).ConfigureAwait(false);

                await UserContextTickerEnqueue.EnqueueMemoryExtractionAsync(
                    timeTickerManager,
                    userId,
                    conversationId,
                    userContent,
                    assistantContent,
                    CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // Background enqueue is best-effort; persistence already succeeded.
            }
        }
    }
}
