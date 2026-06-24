namespace ProjectBrain.Domain.Dtos;

/// <summary>Structured trace envelope logged once per chat turn for debugging and audit.</summary>
public sealed class ChatTurnTraceEnvelope
{
    public required string CorrelationId { get; init; }
    public Guid ConversationId { get; init; }
    public required string UserId { get; init; }
    public ChatTurnRetrievalTrace Retrieval { get; init; } = new();
    public ChatTurnMemoryTrace Memory { get; init; } = new();
    public ChatTurnPromptTrace Prompt { get; init; } = new();
}

public sealed class ChatTurnRetrievalTrace
{
    public int CitationCount { get; init; }
    public IReadOnlyList<string> CitationIds { get; init; } = Array.Empty<string>();
    public string RetrievalMode { get; init; } = "vector";
}

public sealed class ChatTurnMemoryTrace
{
    public IReadOnlyList<string> PoliciesApplied { get; init; } = Array.Empty<string>();
    public bool HasUserPreferences { get; init; }
    public bool HasConversationSummary { get; init; }
    public int SummaryLength { get; init; }
    public int RecentHistoryCount { get; init; }
    public IReadOnlyList<string> FactIdsRetrieved { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> EpisodeIdsRetrieved { get; init; } = Array.Empty<string>();
    public string MemoryRetrievalMode { get; init; } = "disabled";
}

public sealed class ChatTurnPromptTrace
{
    public int EstimatedTokens { get; init; }
    public int MaxTotalTokens { get; init; }
    public bool TruncatedSources { get; init; }
}
