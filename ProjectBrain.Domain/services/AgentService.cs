namespace ProjectBrain.Domain;

using System.Linq;
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
    private readonly IGoalService _goalService;
    private readonly IAgentActionTrackingService _actionTrackingService;
    private readonly IAgentOpenAIService _agentOpenAI;
    private readonly ILogger<AgentService> _logger;

    public AgentService(
        IAgentOrchestrator orchestrator,
        IGoalService goalService,
        IAgentActionTrackingService actionTrackingService,
        IAgentOpenAIService agentOpenAI,
        ILogger<AgentService> logger)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _goalService = goalService ?? throw new ArgumentNullException(nameof(goalService));
        _actionTrackingService = actionTrackingService ?? throw new ArgumentNullException(nameof(actionTrackingService));
        _agentOpenAI = agentOpenAI ?? throw new ArgumentNullException(nameof(agentOpenAI));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public List<Dictionary<string, object>> GetAvailableTools()
    {
        return AgentTools.GetAllToolDefinitions();
    }

    public async Task<AgentResponse> ProcessAgentInteractionAsync(
        string userId,
        string userMessage,
        Guid? conversationId,
        Guid? workflowId,
        string userInformation,
        string userName,
        List<AgentChatMessage> conversationHistory,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing agent interaction for user {UserId}, workflow {WorkflowId}", userId, workflowId);

        // Load or create workflow
        AgentWorkflowState? workflowState = null;
        if (workflowId.HasValue)
        {
            workflowState = await _orchestrator.LoadWorkflowAsync(workflowId.Value, userId, cancellationToken);
            if (workflowState == null)
            {
                _logger.LogWarning("Workflow {WorkflowId} not found, creating new workflow", workflowId);
                workflowState = await _orchestrator.CreateWorkflowAsync(userId, conversationId, "agent_interaction", cancellationToken);
            }
        }
        else
        {
            workflowState = await _orchestrator.CreateWorkflowAsync(userId, conversationId, "agent_interaction", cancellationToken);
        }

        var response = new AgentResponse
        {
            WorkflowId = workflowState.Id,
            Status = "completed"
        };

        try
        {
            // Get recent actions for context
            var recentActions = await _actionTrackingService.GetRecentActionsAsync(userId, 5, cancellationToken);
            var recentActionsContext = string.Join(", ", recentActions.Select(a => $"{a.ToolName} at {a.ExecutedAt:HH:mm}"));

            // Add recent actions to user information if available
            if (!string.IsNullOrEmpty(recentActionsContext))
            {
                userInformation += $"\n\nRecent agent actions: {recentActionsContext}";
            }

            // Get available tools
            var tools = GetAvailableTools();

            // Get agent response with function calling
            var agentResponseEnumerable = _agentOpenAI.GetAgentResponseAsync(
                userMessage,
                userId,
                userInformation,
                userName,
                conversationHistory,
                tools,
                cancellationToken);

            // Process the streaming response and handle tool calls
            var maxToolIterations = 10; // Prevent infinite loops
            var iteration = 0;
            var messages = new List<AgentChatMessage>(conversationHistory);
            messages.Add(new AgentChatMessage { Role = AgentChatMessageRole.User, Content = userMessage });

            while (iteration < maxToolIterations)
            {
                iteration++;
                _logger.LogInformation("Agent iteration {Iteration}", iteration);

                // Collect tool calls from the response
                var toolCalls = new List<(string ToolCallId, string FunctionName, Dictionary<string, object> Parameters)>();
                var assistantMessage = new StringBuilder();

                await foreach (var update in agentResponseEnumerable)
                {
                    if (update.Text != null)
                    {
                        assistantMessage.Append(update.Text);
                    }

                    foreach (var toolCall in update.ToolCalls)
                    {
                        toolCalls.Add((toolCall.ToolCallId, toolCall.FunctionName, toolCall.Parameters));
                    }
                }

                // Add assistant message to history and response
                if (assistantMessage.Length > 0)
                {
                    var messageContent = assistantMessage.ToString();
                    messages.Add(new AgentChatMessage
                    {
                        Role = AgentChatMessageRole.Assistant,
                        Content = messageContent
                    });
                    // Set the response message (will be overwritten if there are multiple iterations)
                    response.Message = messageContent;
                }

                // If no tool calls, we're done
                if (toolCalls.Count == 0)
                {
                    break;
                }

                // Execute tools
                foreach (var (toolCallId, functionName, parameters) in toolCalls)
                {
                    try
                    {
                        _logger.LogInformation("Executing tool: {FunctionName} with parameters: {Parameters}", functionName, JsonSerializer.Serialize(parameters));

                        // Execute the tool
                        var toolResult = await AgentTools.ExecuteTool(functionName, _goalService, userId, parameters, cancellationToken);

                        // Track the action
                        await _actionTrackingService.RecordActionAsync(
                            userId,
                            conversationId,
                            workflowState.Id,
                            functionName,
                            parameters,
                            toolResult,
                            true,
                            null,
                            cancellationToken);

                        // Record in workflow
                        workflowState.ToolExecutionHistory.Add(new ToolExecutionRecord
                        {
                            ToolName = functionName,
                            Parameters = parameters,
                            Result = toolResult,
                            Success = true,
                            ExecutedAt = DateTime.UtcNow
                        });

                        response.ExecutedTools.Add(workflowState.ToolExecutionHistory.Last());

                        // Create function message for next iteration
                        var functionMessage = _agentOpenAI.CreateFunctionMessage(toolCallId, functionName, toolResult);

                        // Note: We would need to add this to the conversation for the next iteration
                        // For now, we'll update the workflow state and continue
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing tool {FunctionName}", functionName);

                        var errorResult = new { success = false, error = ex.Message };

                        // Track failed action
                        await _actionTrackingService.RecordActionAsync(
                            userId,
                            conversationId,
                            workflowState.Id,
                            functionName,
                            parameters,
                            errorResult,
                            false,
                            ex.Message,
                            cancellationToken);

                        // Record in workflow
                        workflowState.ToolExecutionHistory.Add(new ToolExecutionRecord
                        {
                            ToolName = functionName,
                            Parameters = parameters,
                            Result = errorResult,
                            Success = false,
                            ErrorMessage = ex.Message,
                            ExecutedAt = DateTime.UtcNow
                        });

                        response.ExecutedTools.Add(workflowState.ToolExecutionHistory.Last());
                    }
                }

                // Update workflow state
                workflowState.CurrentStep++;
                await _orchestrator.UpdateWorkflowStateAsync(workflowState, cancellationToken);

                // If we executed tools, we need another iteration to get the agent's response
                // For now, we'll break after first iteration to avoid complexity
                // In a full implementation, we'd continue the loop with the function results
                break;
            }

            // Complete workflow
            await _orchestrator.CompleteWorkflowAsync(workflowState, cancellationToken);
            response.Status = "completed";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing agent interaction");
            if (workflowState != null)
            {
                await _orchestrator.FailWorkflowAsync(workflowState, ex.Message, cancellationToken);
            }
            response.Status = "failed";
            response.ErrorMessage = ex.Message;
        }

        return response;
    }
}

