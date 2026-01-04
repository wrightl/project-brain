namespace ProjectBrain.Domain;

/// <summary>
/// Interface for agent workflow orchestration and state management
/// </summary>
public interface IAgentOrchestrator
{
    /// <summary>
    /// Creates a new workflow for agent execution
    /// </summary>
    Task<AgentWorkflowState> CreateWorkflowAsync(string userId, Guid? conversationId, string workflowType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads an existing workflow by ID
    /// </summary>
    Task<AgentWorkflowState?> LoadWorkflowAsync(Guid workflowId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates workflow state (persists if needed for long-running workflows)
    /// </summary>
    Task UpdateWorkflowStateAsync(AgentWorkflowState workflowState, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses a workflow and persists state to database
    /// </summary>
    Task PauseWorkflowAsync(AgentWorkflowState workflowState, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes a paused workflow
    /// </summary>
    Task<AgentWorkflowState?> ResumeWorkflowAsync(Guid workflowId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a workflow
    /// </summary>
    Task CompleteWorkflowAsync(AgentWorkflowState workflowState, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a workflow as failed
    /// </summary>
    Task FailWorkflowAsync(AgentWorkflowState workflowState, string errorMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active workflows for a user
    /// </summary>
    Task<IEnumerable<AgentWorkflowState>> GetActiveWorkflowsAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the state of an agent workflow
/// </summary>
public class AgentWorkflowState
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid? ConversationId { get; set; }
    public string WorkflowType { get; set; } = string.Empty;
    public string Status { get; set; } = "active"; // "active", "paused", "completed", "failed"
    public Dictionary<string, object> CurrentState { get; set; } = new();
    public List<ToolExecutionRecord> ToolExecutionHistory { get; set; } = new();
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }

    // In-memory state for active workflows
    public Queue<PendingToolCall> PendingToolCalls { get; set; } = new();
    public Dictionary<string, object> ToolResultsCache { get; set; } = new();
    public Dictionary<string, object> ConversationContext { get; set; } = new();
}

/// <summary>
/// Represents a pending tool call in the workflow
/// </summary>
public class PendingToolCall
{
    public string ToolName { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a record of a tool execution
/// </summary>
public class ToolExecutionRecord
{
    public string ToolName { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public object? Result { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}

