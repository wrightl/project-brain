namespace ProjectBrain.AI;

using ProjectBrain.Domain.Dtos;

public static class ChatTurnTraceBuilder
{
    public static ChatTurnTraceEnvelope Build(
        string correlationId,
        Guid conversationId,
        string userId,
        ChatMemoryContext memoryContext,
        int recentHistoryCount,
        int citationCount,
        IReadOnlyList<string> citationIds,
        string retrievalMode,
        int estimatedTokens,
        int maxTotalTokens,
        bool truncatedSources,
        IReadOnlyList<PromptSlotTrace> slotTraces)
    {
        return new ChatTurnTraceEnvelope
        {
            CorrelationId = correlationId,
            ConversationId = conversationId,
            UserId = userId,
            Retrieval = new ChatTurnRetrievalTrace
            {
                CitationCount = citationCount,
                CitationIds = citationIds,
                RetrievalMode = retrievalMode
            },
            Memory = new ChatTurnMemoryTrace
            {
                PoliciesApplied = memoryContext.Policies.Select(p => p.Key).ToList(),
                HasUserPreferences = memoryContext.UserPreferences != null,
                HasConversationSummary = memoryContext.EnableConversationSummary
                    && !string.IsNullOrWhiteSpace(memoryContext.ConversationSummary),
                SummaryLength = memoryContext.ConversationSummary?.Length ?? 0,
                RecentHistoryCount = recentHistoryCount,
                FactIdsRetrieved = memoryContext.Facts.Select(f => f.Id.ToString()).ToList(),
                EpisodeIdsRetrieved = memoryContext.Episodes.Select(e => e.Id.ToString()).ToList(),
                MemoryRetrievalMode = memoryContext.MemoryRetrievalMode
            },
            Prompt = new ChatTurnPromptTrace
            {
                EstimatedTokens = estimatedTokens,
                MaxTotalTokens = maxTotalTokens,
                TruncatedSources = truncatedSources,
                Slots = slotTraces
            }
        };
    }

    public static void Log(ILogger logger, ChatTurnTraceEnvelope envelope, string channel = "ChatTrace")
    {
        logger.LogInformation(
            "[{Channel}] {TraceEnvelope}",
            channel,
            System.Text.Json.JsonSerializer.Serialize(envelope));
    }
}
