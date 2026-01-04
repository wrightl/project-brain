namespace ProjectBrain.Domain;

using ProjectBrain.Database.Models;

/// <summary>
/// Service interface for tracking agent actions
/// </summary>
public interface IAgentActionTrackingService
{
    /// <summary>
    /// Records an agent action
    /// </summary>
    Task RecordActionAsync(
        string userId,
        Guid? conversationId,
        Guid? workflowId,
        string toolName,
        Dictionary<string, object> toolParameters,
        object toolResult,
        bool success,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent actions for a user (for context)
    /// </summary>
    Task<IEnumerable<AgentAction>> GetRecentActionsAsync(string userId, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated actions for a user
    /// </summary>
    Task<IEnumerable<AgentAction>> GetActionsByUserIdAsync(string userId, int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets actions by tool name (for analytics)
    /// </summary>
    Task<IEnumerable<AgentAction>> GetActionsByToolNameAsync(string toolName, int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts usage of a specific tool
    /// </summary>
    Task<int> CountByToolAsync(string toolName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets actions by workflow ID
    /// </summary>
    Task<IEnumerable<AgentAction>> GetActionsByWorkflowIdAsync(Guid workflowId, CancellationToken cancellationToken = default);
}

