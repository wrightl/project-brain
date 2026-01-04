namespace ProjectBrain.Domain;

using ProjectBrain.Domain.Dtos;

/// <summary>
/// Service interface for AI agent interactions
/// </summary>
public interface IAgentService
{
    /// <summary>
    /// Processes an agent interaction with streaming response
    /// </summary>
    Task<AgentResponse> ProcessAgentInteractionAsync(
        string userId,
        string userMessage,
        Guid? conversationId,
        Guid? workflowId,
        string userInformation,
        string userName,
        List<AgentChatMessage> conversationHistory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available tools for the agent
    /// </summary>
    List<Dictionary<string, object>> GetAvailableTools();
}

/// <summary>
/// Response from agent processing
/// </summary>
public class AgentResponse
{
    public Guid? WorkflowId { get; set; }
    public string Status { get; set; } = "completed"; // "completed", "paused", "failed"
    public string? ErrorMessage { get; set; }
    public string? Message { get; set; } // The assistant's text response
    public List<ToolExecutionRecord> ExecutedTools { get; set; } = new();
}

