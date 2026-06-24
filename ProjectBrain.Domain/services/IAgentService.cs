namespace ProjectBrain.Domain;

using ProjectBrain.Domain.Dtos;

/// <summary>
/// Service interface for AI agent interactions
/// </summary>
public interface IAgentService
{
  /// <summary>
  /// Streams agent interaction events including text chunks, tool results, and action cards.
  /// </summary>
  IAsyncEnumerable<AgentStreamEvent> StreamAgentInteractionAsync(
      string userId,
      string userMessage,
      Guid? conversationId,
      Guid? workflowId,
      string userInformation,
      string userName,
      List<AgentChatMessage> conversationHistory,
      ChatMemoryContext memoryContext,
      CancellationToken cancellationToken = default);

  /// <summary>
  /// Processes an agent interaction and collects the final response.
  /// </summary>
  Task<AgentResponse> ProcessAgentInteractionAsync(
      string userId,
      string userMessage,
      Guid? conversationId,
      Guid? workflowId,
      string userInformation,
      string userName,
      List<AgentChatMessage> conversationHistory,
      ChatMemoryContext memoryContext,
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
    public List<ChatCitationDto> Citations { get; set; } = new();
}
