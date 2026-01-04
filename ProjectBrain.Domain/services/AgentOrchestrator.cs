namespace ProjectBrain.Domain;

using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;
using ProjectBrain.Database.Models;

/// <summary>
/// Orchestrator for managing agent workflow state and execution
/// </summary>
public class AgentOrchestrator : IAgentOrchestrator
{
    private readonly IAgentWorkflowRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AgentOrchestrator> _logger;

    public AgentOrchestrator(
        IAgentWorkflowRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<AgentOrchestrator> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AgentWorkflowState> CreateWorkflowAsync(string userId, Guid? conversationId, string workflowType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new workflow for user {UserId}, type {WorkflowType}", userId, workflowType);

        var workflow = new AgentWorkflow
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ConversationId = conversationId,
            WorkflowType = workflowType,
            Status = "active",
            CurrentState = "{}",
            ToolExecutionHistory = "[]",
            CurrentStep = 0,
            TotalSteps = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _repository.Add(workflow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToWorkflowState(workflow);
    }

    public async Task<AgentWorkflowState?> LoadWorkflowAsync(Guid workflowId, string userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading workflow {WorkflowId} for user {UserId}", workflowId, userId);

        var workflow = await _repository.GetByIdForUserAsync(workflowId, userId, cancellationToken);
        if (workflow == null)
        {
            _logger.LogWarning("Workflow {WorkflowId} not found for user {UserId}", workflowId, userId);
            return null;
        }

        return ToWorkflowState(workflow);
    }

    public async Task UpdateWorkflowStateAsync(AgentWorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating workflow state for workflow {WorkflowId}", workflowState.Id);

        var workflow = await _repository.GetByIdAsync(workflowState.Id, cancellationToken);
        if (workflow == null)
        {
            _logger.LogWarning("Workflow {WorkflowId} not found for update", workflowState.Id);
            return;
        }

        workflow.Status = workflowState.Status;
        workflow.CurrentState = JsonSerializer.Serialize(workflowState.CurrentState);
        workflow.ToolExecutionHistory = JsonSerializer.Serialize(workflowState.ToolExecutionHistory);
        workflow.CurrentStep = workflowState.CurrentStep;
        workflow.TotalSteps = workflowState.TotalSteps;
        workflow.UpdatedAt = DateTime.UtcNow;
        workflow.ErrorMessage = workflowState.ErrorMessage;

        _repository.Update(workflow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task PauseWorkflowAsync(AgentWorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Pausing workflow {WorkflowId}", workflowState.Id);

        workflowState.Status = "paused";
        await UpdateWorkflowStateAsync(workflowState, cancellationToken);
    }

    public async Task<AgentWorkflowState?> ResumeWorkflowAsync(Guid workflowId, string userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Resuming workflow {WorkflowId} for user {UserId}", workflowId, userId);

        var workflow = await _repository.GetByIdForUserAsync(workflowId, userId, cancellationToken);
        if (workflow == null)
        {
            _logger.LogWarning("Workflow {WorkflowId} not found for resume", workflowId);
            return null;
        }

        if (workflow.Status != "paused")
        {
            _logger.LogWarning("Workflow {WorkflowId} is not paused, cannot resume. Current status: {Status}", workflowId, workflow.Status);
            return null;
        }

        var workflowState = ToWorkflowState(workflow);
        workflowState.Status = "active";
        await UpdateWorkflowStateAsync(workflowState, cancellationToken);

        return workflowState;
    }

    public async Task CompleteWorkflowAsync(AgentWorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Completing workflow {WorkflowId}", workflowState.Id);

        workflowState.Status = "completed";
        workflowState.CompletedAt = DateTime.UtcNow;
        await UpdateWorkflowStateAsync(workflowState, cancellationToken);
    }

    public async Task FailWorkflowAsync(AgentWorkflowState workflowState, string errorMessage, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Failing workflow {WorkflowId} with error: {ErrorMessage}", workflowState.Id, errorMessage);

        workflowState.Status = "failed";
        workflowState.ErrorMessage = errorMessage;
        await UpdateWorkflowStateAsync(workflowState, cancellationToken);
    }

    public async Task<IEnumerable<AgentWorkflowState>> GetActiveWorkflowsAsync(string userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting active workflows for user {UserId}", userId);

        var workflows = await _repository.GetActiveWorkflowsAsync(userId, cancellationToken);
        return workflows.Select(ToWorkflowState);
    }

    private AgentWorkflowState ToWorkflowState(AgentWorkflow workflow)
    {
        var state = new AgentWorkflowState
        {
            Id = workflow.Id,
            UserId = workflow.UserId,
            ConversationId = workflow.ConversationId,
            WorkflowType = workflow.WorkflowType,
            Status = workflow.Status,
            CurrentStep = workflow.CurrentStep,
            TotalSteps = workflow.TotalSteps,
            CreatedAt = workflow.CreatedAt,
            UpdatedAt = workflow.UpdatedAt,
            CompletedAt = workflow.CompletedAt,
            ErrorMessage = workflow.ErrorMessage
        };

        // Deserialize JSON fields
        try
        {
            if (!string.IsNullOrWhiteSpace(workflow.CurrentState))
            {
                state.CurrentState = JsonSerializer.Deserialize<Dictionary<string, object>>(workflow.CurrentState) ?? new Dictionary<string, object>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize CurrentState for workflow {WorkflowId}", workflow.Id);
            state.CurrentState = new Dictionary<string, object>();
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(workflow.ToolExecutionHistory))
            {
                state.ToolExecutionHistory = JsonSerializer.Deserialize<List<ToolExecutionRecord>>(workflow.ToolExecutionHistory) ?? new List<ToolExecutionRecord>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize ToolExecutionHistory for workflow {WorkflowId}", workflow.Id);
            state.ToolExecutionHistory = new List<ToolExecutionRecord>();
        }

        return state;
    }
}

