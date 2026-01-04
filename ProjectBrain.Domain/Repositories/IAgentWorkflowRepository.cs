namespace ProjectBrain.Domain.Repositories;

using ProjectBrain.Database.Models;

/// <summary>
/// Repository interface for AgentWorkflow entity
/// </summary>
public interface IAgentWorkflowRepository : IRepository<AgentWorkflow, Guid>
{
    /// <summary>
    /// Gets a workflow by ID for a specific user
    /// </summary>
    Task<AgentWorkflow?> GetByIdForUserAsync(Guid id, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active workflows for a user
    /// </summary>
    Task<IEnumerable<AgentWorkflow>> GetActiveWorkflowsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all paused workflows that can be resumed
    /// </summary>
    Task<IEnumerable<AgentWorkflow>> GetPausedWorkflowsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets workflows by status for a user
    /// </summary>
    Task<IEnumerable<AgentWorkflow>> GetByStatusAsync(string userId, string status, CancellationToken cancellationToken = default);
}

