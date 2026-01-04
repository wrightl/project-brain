namespace ProjectBrain.Domain;

using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;
using ProjectBrain.Database.Models;

/// <summary>
/// Service for tracking and querying agent actions
/// </summary>
public class AgentActionTrackingService : IAgentActionTrackingService
{
    private readonly IAgentActionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AgentActionTrackingService> _logger;

    public AgentActionTrackingService(
        IAgentActionRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<AgentActionTrackingService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task RecordActionAsync(
        string userId,
        Guid? conversationId,
        Guid? workflowId,
        string toolName,
        Dictionary<string, object> toolParameters,
        object toolResult,
        bool success,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Recording agent action: Tool={ToolName}, User={UserId}, Success={Success}", toolName, userId, success);

        var action = new AgentAction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ConversationId = conversationId,
            WorkflowId = workflowId,
            ToolName = toolName,
            ToolParameters = JsonSerializer.Serialize(toolParameters),
            ToolResult = JsonSerializer.Serialize(toolResult),
            Success = success,
            ErrorMessage = errorMessage,
            ExecutedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _repository.Add(action);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<AgentAction>> GetRecentActionsAsync(string userId, int count, CancellationToken cancellationToken = default)
    {
        return await _repository.GetRecentActionsAsync(userId, count, cancellationToken);
    }

    public async Task<IEnumerable<AgentAction>> GetActionsByUserIdAsync(string userId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByUserIdAsync(userId, skip, take, cancellationToken);
    }

    public async Task<IEnumerable<AgentAction>> GetActionsByToolNameAsync(string toolName, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByToolNameAsync(toolName, skip, take, cancellationToken);
    }

    public async Task<int> CountByToolAsync(string toolName, CancellationToken cancellationToken = default)
    {
        return await _repository.CountByToolAsync(toolName, cancellationToken);
    }

    public async Task<IEnumerable<AgentAction>> GetActionsByWorkflowIdAsync(Guid workflowId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByWorkflowIdAsync(workflowId, cancellationToken);
    }
}

