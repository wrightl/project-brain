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

    public async Task<AgentResponse> ProcessAgentInteractionAsync(
        string userId,
        string userMessage,
        Guid? conversationId,
        Guid? workflowId,
        string userInformation,
        string userName,
        List<AgentChatMessage> conversationHistory,
        ChatMemoryContext memoryContext,
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
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Streaming agent interaction for user {UserId}, workflow {WorkflowId}", userId, workflowId);

        AgentWorkflowState? workflowState = null;
        workflowState = await LoadOrCreateWorkflowAsync(userId, conversationId, workflowId, cancellationToken);
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

        var tools = GetAvailableTools();
        var toolContext = _toolContextFactory.Create(userId, conversationId, workflowState.Id, userMessage);

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

                foreach (var toolCall in validToolCalls)
                {
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
            }

            if (iterationTools.Count > 0)
            {
                yield return new AgentStreamEvent { Type = "tools_executed", Value = iterationTools };
            }

                _agentOpenAI.AppendToolResults(session, assistantText, validToolCalls, toolResults);

            workflowState.CurrentStep++;
            await _orchestrator.UpdateWorkflowStateAsync(workflowState, cancellationToken);
        }

        await _orchestrator.CompleteWorkflowAsync(workflowState, cancellationToken);
        yield return new AgentStreamEvent
        {
            Type = "status",
            Value = new AgentStreamStatus { Status = "completed" }
        };
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
