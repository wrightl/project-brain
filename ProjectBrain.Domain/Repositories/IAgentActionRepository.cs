namespace ProjectBrain.Domain.Repositories;

using ProjectBrain.Database.Models;

/// <summary>
/// Repository interface for AgentAction entity
/// </summary>
public interface IAgentActionRepository : IRepository<AgentAction, Guid>
{
    /// <summary>
    /// Gets paginated actions for a user
    /// </summary>
    Task<IEnumerable<AgentAction>> GetByUserIdAsync(string userId, int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets actions by tool name (for analytics)
    /// </summary>
    Task<IEnumerable<AgentAction>> GetByToolNameAsync(string toolName, int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent actions for a user (for context)
    /// </summary>
    Task<IEnumerable<AgentAction>> GetRecentActionsAsync(string userId, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts usage of a specific tool
    /// </summary>
    Task<int> CountByToolAsync(string toolName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets actions by workflow ID
    /// </summary>
    Task<IEnumerable<AgentAction>> GetByWorkflowIdAsync(Guid workflowId, CancellationToken cancellationToken = default);
}

