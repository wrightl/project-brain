namespace ProjectBrain.Domain;

using ProjectBrain.Domain.Dtos;

/// <summary>
/// Interface for agent-specific Azure OpenAI operations
/// </summary>
public interface IAgentOpenAIService
{
    /// <summary>
    /// Prepares a new agent session with the initial prompt and message list.
    /// </summary>
    Task<AgentSession> BeginSessionAsync(
        AgentSessionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams one model turn for the given session (initial or continuation).
    /// </summary>
    IAsyncEnumerable<AgentStreamingUpdate> StreamTurnAsync(
        AgentSession session,
        List<Dictionary<string, object>> tools,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends assistant tool-call message and tool results for the next turn.
    /// </summary>
    void AppendToolResults(
        AgentSession session,
        string? assistantText,
        IReadOnlyList<AgentToolCall> toolCalls,
        IReadOnlyList<AgentToolResult> toolResults);

    /// <summary>
    /// Legacy first-turn streaming (delegates to BeginSession + StreamTurn).
    /// </summary>
    IAsyncEnumerable<AgentStreamingUpdate> GetAgentResponseAsync(
        string userQuery,
        string userId,
        string userInformation,
        string userName,
        List<AgentChatMessage> history,
        ChatMemoryContext memoryContext,
        List<Dictionary<string, object>> tools,
        Guid? conversationId = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a function message for tool execution results (SDK-specific; used internally).
    /// </summary>
    object CreateFunctionMessage(string toolCallId, string functionName, object result);
}

/// <summary>
/// Wrapper for streaming update information
/// </summary>
public class AgentStreamingUpdate
{
    public string? Text { get; set; }
    public List<AgentToolCall> ToolCalls { get; set; } = new();
}

/// <summary>
/// Wrapper for tool call information
/// </summary>
public class AgentToolCall
{
    public string ToolCallId { get; set; } = string.Empty;
    public string FunctionName { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}
