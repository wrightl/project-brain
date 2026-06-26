namespace ProjectBrain.Domain;

using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProjectBrain.Domain.Dtos;

/// <summary>
/// Service implementation for AI agent interactions
/// </summary>
public class AgentService : IAgentService
{
    private readonly IAgentOrchestrator _orchestrator;
    private readonly IAgentToolRegistry _toolRegistry;
    private readonly IAgentToolContextFactory _toolContextFactory;
    private readonly IAgentActionTrackingService _actionTrackingService;
    private readonly IAgentOpenAIService _agentOpenAI;
    private readonly IChatRetrievalService _chatRetrievalService;
    private readonly ILogger<AgentService> _logger;

    public AgentService(
        IAgentOrchestrator orchestrator,
        IAgentToolRegistry toolRegistry,
        IAgentToolContextFactory toolContextFactory,
        IAgentActionTrackingService actionTrackingService,
        IAgentOpenAIService agentOpenAI,
        IChatRetrievalService chatRetrievalService,
        ILogger<AgentService> logger)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        _toolContextFactory = toolContextFactory ?? throw new ArgumentNullException(nameof(toolContextFactory));
        _actionTrackingService = actionTrackingService ?? throw new ArgumentNullException(nameof(actionTrackingService));
        _agentOpenAI = agentOpenAI ?? throw new ArgumentNullException(nameof(agentOpenAI));
        _chatRetrievalService = chatRetrievalService ?? throw new ArgumentNullException(nameof(chatRetrievalService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public List<Dictionary<string, object>> GetAvailableTools()
    {
        return _toolRegistry.GetAllDefinitions().ToList();
    }

    public async Task<List<Dictionary<string, object>>> GetEnabledToolsAsync(
        string userId,
        Guid? conversationId = null,
        Guid? workflowId = null,
        UserType userType = UserType.User,
        CancellationToken cancellationToken = default)
    {
        var context = _toolContextFactory.Create(userId, conversationId, workflowId, userType: userType);
        var tools = await _toolRegistry.GetEnabledDefinitionsAsync(context, cancellationToken);
        return tools.ToList();
    }

    public async Task<AgentPendingActionResult> ConfirmPendingActionAsync(
        string userId,
        Guid workflowId,
        Guid actionId,
        CancellationToken cancellationToken = default)
    {
        var workflowState = await _orchestrator.LoadWorkflowAsync(workflowId, userId, cancellationToken);
        if (workflowState is null)
        {
            return new AgentPendingActionResult { Success = false, Message = "Workflow not found" };
        }

        var pendingAction = AgentPendingActionStore.Find(workflowState, actionId);
        if (pendingAction is null)
        {
            return new AgentPendingActionResult { Success = false, Message = "Pending action not found" };
        }

        var toolContext = _toolContextFactory.Create(
            userId,
            workflowState.ConversationId,
            workflowState.Id,
            userType: UserType.User);

        try
        {
            var toolResult = await _toolRegistry.ExecuteAsync(
                pendingAction.ToolName,
                toolContext,
                pendingAction.Parameters,
                cancellationToken);

            await _actionTrackingService.RecordActionAsync(
                userId,
                workflowState.ConversationId,
                workflowState.Id,
                pendingAction.ToolName,
                pendingAction.Parameters,
                toolResult,
                true,
                null,
                cancellationToken);

            var record = new ToolExecutionRecord
            {
                ToolName = pendingAction.ToolName,
                Parameters = pendingAction.Parameters,
                Result = toolResult,
                Success = true,
                ExecutedAt = DateTime.UtcNow
            };

            workflowState.ToolExecutionHistory.Add(record);
            AgentPendingActionStore.Remove(workflowState, actionId);
            workflowState.Status = "active";
            await _orchestrator.UpdateWorkflowStateAsync(workflowState, cancellationToken);

            return new AgentPendingActionResult
            {
                Success = true,
                Message = "Action confirmed and executed",
                ToolExecution = record
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming pending action {ActionId}", actionId);
            return new AgentPendingActionResult { Success = false, Message = ex.Message };
        }
    }

    public async Task<AgentPendingActionResult> CancelPendingActionAsync(
        string userId,
        Guid workflowId,
        Guid actionId,
        CancellationToken cancellationToken = default)
    {
        var workflowState = await _orchestrator.LoadWorkflowAsync(workflowId, userId, cancellationToken);
        if (workflowState is null)
        {
            return new AgentPendingActionResult { Success = false, Message = "Workflow not found" };
        }

        var pendingAction = AgentPendingActionStore.Find(workflowState, actionId);
        if (pendingAction is null)
        {
            return new AgentPendingActionResult { Success = false, Message = "Pending action not found" };
        }

        AgentPendingActionStore.MarkCancelled(workflowState, actionId);
        AgentPendingActionStore.Remove(workflowState, actionId);
        workflowState.Status = "active";
        await _orchestrator.UpdateWorkflowStateAsync(workflowState, cancellationToken);

        return new AgentPendingActionResult
        {
            Success = true,
            Message = "Action cancelled"
        };
    }

    public async Task<AgentResponse> ProcessAgentInteractionAsync(
        string userId,
        string userMessage,
        Guid? conversationId,
        Guid? workflowId,
        string userInformation,
        string userName,
        List<AgentChatMessage> conversationHistory,
        ChatMemoryContext memoryContext,
        UserType userType = UserType.User,
        CancellationToken cancellationToken = default)
    {
        var response = new AgentResponse();
        await foreach (var streamEvent in StreamAgentInteractionAsync(
            userId,
            userMessage,
            conversationId,
            workflowId,
            userInformation,
            userName,
            conversationHistory,
            memoryContext,
            userType,
            cancellationToken))
        {
            switch (streamEvent.Type)
            {
                case "workflow":
                    if (streamEvent.Value is Guid wfId)
                    {
                        response.WorkflowId = wfId;
                    }
                    else if (streamEvent.Value is JsonElement wfElement && wfElement.TryGetProperty("id", out var idProp))
                    {
                        response.WorkflowId = Guid.Parse(idProp.GetString()!);
                    }

                    break;

                case "text":
                    response.Message = (response.Message ?? string.Empty) + streamEvent.Value?.ToString();
                    break;

                case "tools_executed":
                    if (streamEvent.Value is List<ToolExecutionRecord> tools)
                    {
                        response.ExecutedTools.AddRange(tools);
                    }

                    break;

                case "citations":
                    if (streamEvent.Value is List<ChatCitationDto> citations)
                    {
                        response.Citations = citations;
                    }

                    break;

                case "status":
                    if (streamEvent.Value is AgentStreamStatus status)
                    {
                        response.Status = status.Status;
                        response.ErrorMessage = status.Error;
                    }

                    break;
            }
        }

        return response;
    }

    public async IAsyncEnumerable<AgentStreamEvent> StreamAgentInteractionAsync(
        string userId,
        string userMessage,
        Guid? conversationId,
        Guid? workflowId,
        string userInformation,
        string userName,
        List<AgentChatMessage> conversationHistory,
        ChatMemoryContext memoryContext,
        UserType userType = UserType.User,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Streaming agent interaction for user {UserId}, workflow {WorkflowId}", userId, workflowId);

        var workflowState = await LoadOrCreateWorkflowAsync(userId, conversationId, workflowId, cancellationToken);
        yield return new AgentStreamEvent
        {
            Type = "workflow",
            Value = new { id = workflowState.Id }
        };

        var recentActions = await _actionTrackingService.GetRecentActionsAsync(userId, 5, cancellationToken);
        var recentActionsContext = string.Join(", ", recentActions.Select(a => $"{a.ToolName} at {a.ExecutedAt:HH:mm}"));
        if (!string.IsNullOrEmpty(recentActionsContext))
        {
            userInformation += $"\n\nRecent agent actions: {recentActionsContext}";
        }

        var correlationId = Guid.NewGuid().ToString("N");
        var retrieval = await _chatRetrievalService.RetrieveAsync(
            userMessage,
            userId,
            memoryContext,
            correlationId,
            cancellationToken);

        if (retrieval.Citations.Count > 0)
        {
            yield return new AgentStreamEvent
            {
                Type = "citations",
                Value = retrieval.Citations.ToList()
            };
        }

        var toolContext = _toolContextFactory.Create(userId, conversationId, workflowState.Id, userMessage, userType);
        var tools = (await _toolRegistry.GetEnabledDefinitionsAsync(toolContext, cancellationToken)).ToList();

        var session = await _agentOpenAI.BeginSessionAsync(new AgentSessionRequest
        {
            UserQuery = userMessage,
            UserId = userId,
            UserInformation = userInformation,
            UserName = userName,
            History = conversationHistory,
            MemoryContext = memoryContext,
            ConversationId = conversationId,
            CorrelationId = correlationId,
            SourcesFormatted = retrieval.SourcesFormatted,
            CitationCount = retrieval.Citations.Count,
            CitationIds = retrieval.Citations.Select(c => c.Id).ToList()
        }, cancellationToken);

        const int maxToolIterations = 10;
        var iteration = 0;

        while (iteration < maxToolIterations)
        {
            iteration++;
            _logger.LogInformation("Agent iteration {Iteration}", iteration);

            var toolCalls = new List<AgentToolCall>();
            var assistantMessage = new StringBuilder();

            await foreach (var update in _agentOpenAI.StreamTurnAsync(session, tools, cancellationToken))
            {
                if (update.Text != null)
                {
                    assistantMessage.Append(update.Text);
                    yield return new AgentStreamEvent { Type = "text", Value = update.Text };
                }

                if (update.ToolCalls.Count > 0)
                {
                    toolCalls.AddRange(update.ToolCalls);
                }
            }

            var assistantText = assistantMessage.Length > 0 ? assistantMessage.ToString() : null;

            if (toolCalls.Count == 0)
            {
                break;
            }

            var validToolCalls = toolCalls
                .Where(tc => !string.IsNullOrWhiteSpace(tc.FunctionName))
                .ToList();

            if (validToolCalls.Count == 0)
            {
                _logger.LogWarning("Model returned tool calls without function names; ending turn.");
                break;
            }

            var iterationTools = new List<ToolExecutionRecord>();
            var toolResults = new List<AgentToolResult>();
            var stopAfterUserInput = false;
            var stopAfterPendingConfirmation = false;

            foreach (var toolCall in validToolCalls)
            {
                var handler = _toolRegistry.TryGetHandler(toolCall.FunctionName);
                if (handler?.RequiresConfirmation == true)
                {
                    var pendingActionId = Guid.NewGuid();
                    var preview = handler.BuildConfirmationPreview(toolCall.Parameters);
                    var pendingAction = new AgentPendingAction
                    {
                        Id = pendingActionId,
                        ToolName = toolCall.FunctionName,
                        Parameters = toolCall.Parameters,
                        Preview = preview,
                        Status = "awaiting_confirmation",
                        CreatedAt = DateTime.UtcNow
                    };

                    AgentPendingActionStore.Add(workflowState, pendingAction);
                    await _orchestrator.PauseWorkflowAsync(workflowState, cancellationToken);

                    var pendingResult = new
                    {
                        success = true,
                        status = "pending_confirmation",
                        pendingActionId,
                        workflowId = workflowState.Id,
                        toolName = toolCall.FunctionName,
                        preview
                    };

                    toolResults.Add(new AgentToolResult
                    {
                        ToolCallId = toolCall.ToolCallId,
                        FunctionName = toolCall.FunctionName,
                        Result = pendingResult
                    });

                    yield return new AgentStreamEvent
                    {
                        Type = "pending_action",
                        Value = new
                        {
                            cardType = "pending_confirmation",
                            pendingActionId,
                            workflowId = workflowState.Id,
                            toolName = toolCall.FunctionName,
                            preview,
                            parameters = toolCall.Parameters
                        }
                    };

                    stopAfterPendingConfirmation = true;
                    break;
                }

                ToolExecutionRecord record;
                object? toolResult = null;
                try
                {
                    _logger.LogInformation(
                        "Executing tool: {FunctionName} with parameters: {Parameters}",
                        toolCall.FunctionName,
                        JsonSerializer.Serialize(toolCall.Parameters));

                    toolResult = await _toolRegistry.ExecuteAsync(
                        toolCall.FunctionName,
                        toolContext,
                        toolCall.Parameters,
                        cancellationToken);

                    await _actionTrackingService.RecordActionAsync(
                        userId,
                        conversationId,
                        workflowState.Id,
                        toolCall.FunctionName,
                        toolCall.Parameters,
                        toolResult,
                        true,
                        null,
                        cancellationToken);

                    record = new ToolExecutionRecord
                    {
                        ToolName = toolCall.FunctionName,
                        Parameters = toolCall.Parameters,
                        Result = toolResult,
                        Success = true,
                        ExecutedAt = DateTime.UtcNow
                    };

                    toolResults.Add(new AgentToolResult
                    {
                        ToolCallId = toolCall.ToolCallId,
                        FunctionName = toolCall.FunctionName,
                        Result = toolResult
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing tool {FunctionName}", toolCall.FunctionName);

                    var errorResult = new { success = false, error = ex.Message };

                    await _actionTrackingService.RecordActionAsync(
                        userId,
                        conversationId,
                        workflowState.Id,
                        toolCall.FunctionName,
                        toolCall.Parameters,
                        errorResult,
                        false,
                        ex.Message,
                        cancellationToken);

                    record = new ToolExecutionRecord
                    {
                        ToolName = toolCall.FunctionName,
                        Parameters = toolCall.Parameters,
                        Result = errorResult,
                        Success = false,
                        ErrorMessage = ex.Message,
                        ExecutedAt = DateTime.UtcNow
                    };

                    toolResults.Add(new AgentToolResult
                    {
                        ToolCallId = toolCall.ToolCallId,
                        FunctionName = toolCall.FunctionName,
                        Result = errorResult
                    });
                }

                if (record.Success && handler?.PausesTurn == true && toolResult is not null)
                {
                    var userChoices = ExtractUserChoices(toolResult);
                    if (userChoices is not null)
                    {
                        yield return new AgentStreamEvent { Type = "user_choices", Value = userChoices };
                        stopAfterUserInput = true;
                    }
                }

                if (record.Success && toolCall.FunctionName == "suggest_coping_strategies" && toolResult is not null)
                {
                    var strategies = ExtractStrategies(toolResult);
                    if (strategies.Count > 0)
                    {
                        yield return new AgentStreamEvent { Type = "strategies", Value = strategies };
                    }
                }

                workflowState.ToolExecutionHistory.Add(record);
                iterationTools.Add(record);

                if (stopAfterUserInput)
                {
                    break;
                }
            }

            if (iterationTools.Count > 0)
            {
                yield return new AgentStreamEvent { Type = "tools_executed", Value = iterationTools };
            }

            if (stopAfterUserInput)
            {
                workflowState.CurrentStep++;
                await _orchestrator.UpdateWorkflowStateAsync(workflowState, cancellationToken);
                break;
            }

            if (stopAfterPendingConfirmation)
            {
                workflowState.CurrentStep++;
                await _orchestrator.UpdateWorkflowStateAsync(workflowState, cancellationToken);
                break;
            }

            _agentOpenAI.AppendToolResults(session, assistantText, validToolCalls, toolResults);

            workflowState.CurrentStep++;
            await _orchestrator.UpdateWorkflowStateAsync(workflowState, cancellationToken);
        }

        if (workflowState.Status != "paused")
        {
            await _orchestrator.CompleteWorkflowAsync(workflowState, cancellationToken);
            yield return new AgentStreamEvent
            {
                Type = "status",
                Value = new AgentStreamStatus { Status = "completed" }
            };
        }
        else
        {
            yield return new AgentStreamEvent
            {
                Type = "status",
                Value = new AgentStreamStatus { Status = "paused" }
            };
        }
    }

    private async Task<AgentWorkflowState> LoadOrCreateWorkflowAsync(
        string userId,
        Guid? conversationId,
        Guid? workflowId,
        CancellationToken cancellationToken)
    {
        if (workflowId.HasValue)
        {
            var workflowState = await _orchestrator.LoadWorkflowAsync(workflowId.Value, userId, cancellationToken);
            if (workflowState != null)
            {
                return workflowState;
            }

            _logger.LogWarning("Workflow {WorkflowId} not found, creating new workflow", workflowId);
        }

        return await _orchestrator.CreateWorkflowAsync(userId, conversationId, "agent_interaction", cancellationToken);
    }

    private static object? ExtractUserChoices(object toolResult)
    {
        var json = JsonSerializer.Serialize(toolResult);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("options", out var options) || options.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var parsedOptions = options.EnumerateArray()
            .Select(option => new
            {
                id = option.TryGetProperty("id", out var id) ? id.GetString() : null,
                label = option.TryGetProperty("label", out var label) ? label.GetString() : null
            })
            .Where(option => !string.IsNullOrWhiteSpace(option.id) && !string.IsNullOrWhiteSpace(option.label))
            .ToList();

        if (parsedOptions.Count == 0)
        {
            return null;
        }

        return new
        {
            prompt = root.TryGetProperty("prompt", out var prompt) && prompt.ValueKind == JsonValueKind.String
                ? prompt.GetString()
                : null,
            allowMultiple = root.TryGetProperty("allowMultiple", out var allowMultiple)
                            && allowMultiple.ValueKind == JsonValueKind.True,
            options = parsedOptions
        };
    }

    private static List<object> ExtractStrategies(object toolResult)
    {
        var json = JsonSerializer.Serialize(toolResult);
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("strategies", out var strategies) || strategies.ValueKind != JsonValueKind.Array)
        {
            return new List<object>();
        }

        return strategies.EnumerateArray().Select(s => (object)new
        {
            title = s.TryGetProperty("title", out var title) ? title.GetString() : null,
            description = s.TryGetProperty("description", out var desc) ? desc.GetString() : null,
            iconKey = s.TryGetProperty("iconKey", out var icon) ? icon.GetString() : null
        }).ToList();
    }
}

public sealed class AgentStreamStatus
{
    public required string Status { get; init; }
    public string? Error { get; init; }
}
