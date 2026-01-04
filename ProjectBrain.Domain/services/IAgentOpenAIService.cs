namespace ProjectBrain.Domain;

using ProjectBrain.Domain.Dtos;

/// <summary>
/// Interface for agent-specific Azure OpenAI operations
/// </summary>
public interface IAgentOpenAIService
{
    /// <summary>
    /// Gets a streaming chat response with function calling support
    /// Returns an async enumerable of update objects
    /// </summary>
    IAsyncEnumerable<AgentStreamingUpdate> GetAgentResponseAsync(
        string userQuery,
        string userId,
        string userInformation,
        string userName,
        List<AgentChatMessage> history,
        List<Dictionary<string, object>> tools,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a function message for tool execution results
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

