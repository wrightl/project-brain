namespace ProjectBrain.Domain.Dtos;

/// <summary>
/// Opaque session state for multi-turn agent conversations.
/// SDK message list is stored internally by IAgentOpenAIService implementation.
/// </summary>
public sealed class AgentSession
{
    public object? SdkMessageState { get; set; }
    public bool IsInitialTurn { get; set; } = true;
    public string? CorrelationId { get; init; }
    public Guid? ConversationId { get; init; }
}

public sealed class AgentSessionRequest
{
    public required string UserQuery { get; init; }
    public required string UserId { get; init; }
    public required string UserInformation { get; init; }
    public required string UserName { get; init; }
    public required List<AgentChatMessage> History { get; init; }
    public required ChatMemoryContext MemoryContext { get; init; }
    public Guid? ConversationId { get; init; }
    public string? CorrelationId { get; init; }
    public string SourcesFormatted { get; init; } = string.Empty;
    public int CitationCount { get; init; }
    public IReadOnlyList<string> CitationIds { get; init; } = Array.Empty<string>();
}

public sealed class AgentToolResult
{
    public required string ToolCallId { get; init; }
    public required string FunctionName { get; init; }
    public required object Result { get; init; }
}
