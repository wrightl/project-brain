namespace ProjectBrain.AI;

using System.Linq;
using System.Text;
using System.Text.Json;
using OpenAI;
using OpenAI.Chat;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Dtos;

/// <summary>
/// Implementation of IAgentOpenAIService for agent-specific Azure OpenAI operations
/// </summary>
public class AgentOpenAIService : IAgentOpenAIService
{
    private readonly AzureOpenAIServices _services;

    public AgentOpenAIService(AzureOpenAIServices services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public async IAsyncEnumerable<AgentStreamingUpdate> GetAgentResponseAsync(
        string userQuery,
        string userId,
        string userInformation,
        string userName,
        List<AgentChatMessage> history,
        ChatMemoryContext memoryContext,
        List<Dictionary<string, object>> tools,
        Guid? conversationId = null,
        string? correlationId = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _services.Logger.LogInformation("Starting GetAgentResponseAsync for userQuery: {UserQuery}, userId: {UserId}, userName: {UserName}", userQuery, userId, userName);

        var aiSettings = await _services.ApplicationSettingsService.GetAISettingsAsync();
        var promptBudgetSettings = await _services.ApplicationSettingsService.GetPromptBudgetSettingsAsync();
        var estimator = await TokenEstimatorFactory.CreateAsync(_services.ApplicationSettingsService);
        var includeOnboarding = ChatPromptAssembler.ShouldIncludeOnboardingBlob(
            aiSettings.IncludeFullOnboardingBlob,
            userInformation,
            memoryContext,
            isFirstTurn: history.Count == 0);

        // Build system prompt with memory-aware policies and preferences
        var systemPrompt = BuildAgentSystemPrompt(userName, memoryContext);

        List<AgentChatMessage> limitedHistory;
        string userPrompt;
        IReadOnlyList<PromptSlotTrace> slotTraces = Array.Empty<PromptSlotTrace>();

        if (promptBudgetSettings.EnablePromptBudget)
        {
            var initialHistory = ChatPromptAssembler.SelectRecentAgentHistory(history, memoryContext).ToList();
            var budgeted = PromptTokenBudgetAssembler.AssembleForAgent(
                userQuery,
                userInformation,
                memoryContext,
                initialHistory,
                promptBudgetSettings,
                aiSettings.MaxTotalTokens,
                estimator,
                includeOnboarding);
            userPrompt = budgeted.UserPrompt;
            limitedHistory = budgeted.LimitedHistory.ToList();
            slotTraces = budgeted.SlotTraces;
        }
        else
        {
            limitedHistory = ChatPromptAssembler.SelectRecentAgentHistory(history, memoryContext).ToList();
            userPrompt = ChatPromptAssembler.BuildAgentUserPrompt(
                userQuery,
                userInformation,
                memoryContext,
                includeOnboarding);
        }

        var estimatedTokens = estimator.EstimateTokens(systemPrompt) + estimator.EstimateTokens(userPrompt);
        foreach (var msg in limitedHistory)
        {
            estimatedTokens += estimator.EstimateTokens(msg.Content);
        }

        var traceEnvelope = ChatTurnTraceBuilder.Build(
            correlationId ?? "no-trace",
            conversationId ?? Guid.Empty,
            userId,
            memoryContext,
            limitedHistory.Count,
            citationCount: 0,
            citationIds: Array.Empty<string>(),
            retrievalMode: "agent",
            estimatedTokens,
            aiSettings.MaxTotalTokens,
            truncatedSources: false,
            slotTraces);
        ChatTurnTraceBuilder.Log(_services.Logger, traceEnvelope, "AgentTrace");

        // Create chat messages
        var messages = ToChatMessages(limitedHistory);

        // Add system message
        messages.Insert(0, new SystemChatMessage(systemPrompt));

        // Build user prompt with memory context
        messages.Add(new UserChatMessage(userPrompt));

        // Convert tools to ChatTool format for function calling
        var chatTools = new List<ChatTool>();
        foreach (var toolDef in tools)
        {
            if (toolDef.TryGetValue("type", out var type) && type?.ToString() == "function")
            {
                if (toolDef.TryGetValue("function", out var funcObj) && funcObj is Dictionary<string, object> funcDict)
                {
                    var toolName = funcDict["name"]?.ToString() ?? "";
                    var toolDescription = funcDict.TryGetValue("description", out var desc) ? desc?.ToString() : null;
                    var toolParameters = funcDict.TryGetValue("parameters", out var paramsObj) ? JsonSerializer.Serialize(paramsObj) : "{}";

                    // Create function tool using the SDK's ChatTool.CreateFunctionTool
                    var tool = ChatTool.CreateFunctionTool(toolName, toolDescription, BinaryData.FromString(toolParameters));
                    chatTools.Add(tool);
                }
            }
        }

        // Get streaming response with tools
        // Note: The OpenAI SDK may require tools to be passed differently based on version
        // For now, we'll use the basic streaming API and handle tools in the response
        var chatClient = _services.OpenAIClient.GetChatClient(Constants.CHAT_CLIENT_DEPLOYMENT);

        // TODO: Add tools support when SDK version is confirmed
        // The tools will be detected in the streaming response as tool calls
        var response = chatClient.CompleteChatStreamingAsync(messages);

        // Convert to domain types
        // Track tool calls by ID to aggregate partial updates
        var toolCallsMap = new Dictionary<string, AgentToolCall>();
        var currentText = new StringBuilder();

        await foreach (var update in response)
        {
            var domainUpdate = new AgentStreamingUpdate();

            foreach (var choice in update.ContentUpdate)
            {
                if (choice.Text != null)
                {
                    currentText.Append(choice.Text);
                    domainUpdate.Text = choice.Text; // Send incremental text updates
                }
            }

            // foreach (StreamingChatToolCallUpdate toolUpdate in update.ToolCallUpdates)
            // {
            //     int index = toolUpdate.Index;

            //     if (!toolCallsMap.ContainsKey(index))
            //     {
            //         toolCallsMap[index] = (toolUpdate.ToolCallId, toolUpdate.FunctionName, new StringBuilder());
            //     }

            //     // Concatenate the BinaryData fragment as text
            //     if (toolUpdate.FunctionArgumentsUpdate != null && !toolUpdate.FunctionArgumentsUpdate.IsEmpty)
            //     {
            //         toolCallsMap[index].Arguments.Append(toolUpdate.FunctionArgumentsUpdate.ToString());
            //     }
            // }

            foreach (var toolCall in update.ToolCallUpdates)
            {
                var toolCallId = toolCall.ToolCallId ?? Guid.NewGuid().ToString();

                // Aggregate tool call data across multiple updates
                if (!toolCallsMap.TryGetValue(toolCallId, out var existingCall))
                {
                    existingCall = new AgentToolCall
                    {
                        ToolCallId = toolCallId,
                        FunctionName = toolCall.FunctionName ?? "",
                        Parameters = new Dictionary<string, object>()
                    };
                    toolCallsMap[toolCallId] = existingCall;
                }

                // Append function arguments (they may come in chunks)
                if (toolCall.FunctionArgumentsUpdate != null)
                {
                    try
                    {
                        var newParams = JsonSerializer.Deserialize<Dictionary<string, object>>(BinaryData.FromStream(toolCall.FunctionArgumentsUpdate.ToStream())) ?? new Dictionary<string, object>();
                        foreach (var kvp in newParams)
                        {
                            existingCall.Parameters[kvp.Key] = kvp.Value;
                        }
                    }
                    catch
                    {
                        // Ignore parse errors for partial arguments
                    }
                }

                // Update function name if provided
                if (!string.IsNullOrEmpty(toolCall.FunctionName))
                {
                    existingCall.FunctionName = toolCall.FunctionName;
                }
            }

            // If we have text, yield it
            if (domainUpdate.Text != null)
            {
                yield return domainUpdate;
            }
        }

        // Yield final tool calls if any
        if (toolCallsMap.Count > 0)
        {
            var toolCallUpdate = new AgentStreamingUpdate
            {
                ToolCalls = toolCallsMap.Values.ToList()
            };
            yield return toolCallUpdate;
        }
    }

    public object CreateFunctionMessage(string toolCallId, string functionName, object result)
    {
        var resultJson = JsonSerializer.Serialize(result);
        return new ToolChatMessage(toolCallId, functionName, resultJson);
    }

    private string BuildAgentSystemPrompt(string userName, ChatMemoryContext memoryContext)
    {
        var prompt = new StringBuilder();

        prompt.AppendLine("You are a proactive AI assistant for neurodiverse individuals. You can perform actions on behalf of users to help them manage their daily goals and tasks.");
        prompt.AppendLine();

        var policiesBlock = ChatPromptAssembler.FormatPoliciesBlock(memoryContext.Policies);
        if (!string.IsNullOrWhiteSpace(policiesBlock))
        {
            prompt.AppendLine(policiesBlock);
            prompt.AppendLine();
        }

        if (memoryContext.UserPreferences is { } prefs)
        {
            prompt.AppendLine(ChatPromptAssembler.FormatPreferencesBlock(prefs));
            prompt.AppendLine();
        }

        prompt.AppendLine("Your capabilities:");
        prompt.AppendLine("- Create daily goals when users mention tasks or objectives");
        prompt.AppendLine("- Retrieve and view existing goals");
        prompt.AppendLine("- Mark goals as complete or incomplete");
        prompt.AppendLine("- Help organize and prioritize tasks");
        prompt.AppendLine("- Suggest actions based on conversation context");
        prompt.AppendLine();
        prompt.AppendLine("When appropriate, proactively offer to:");
        prompt.AppendLine("- Create daily goals from user's mentioned tasks");
        prompt.AppendLine("- Organize their day based on their preferences");
        prompt.AppendLine("- Suggest coping strategies as goals");
        prompt.AppendLine();
        prompt.AppendLine("Communication style:");
        prompt.AppendLine("- Be clear, concise, and break down complex information into manageable parts");
        prompt.AppendLine("- Use a friendly, supportive, and respectful tone");
        prompt.AppendLine("- Always explain what actions you're taking and why");
        prompt.AppendLine("- Ask for confirmation before major actions if uncertain");
        prompt.AppendLine();

        if (!string.IsNullOrWhiteSpace(userName))
        {
            prompt.AppendLine($"You are chatting with {userName}. Use their name occasionally and naturally - not in every sentence, and never in a patronizing or condescending way.");
            prompt.AppendLine();
        }

        prompt.AppendLine("When you decide to perform an action, use the available tools. After using a tool, explain what you did to the user in a friendly way.");

        return prompt.ToString();
    }


    private List<ChatMessage> ToChatMessages(List<AgentChatMessage> history)
    {
        var messages = new List<ChatMessage>();
        foreach (var msg in history)
        {
            if (msg.Role == AgentChatMessageRole.User)
            {
                messages.Add(new UserChatMessage(msg.Content));
            }
            else if (msg.Role == AgentChatMessageRole.Assistant)
            {
                messages.Add(new AssistantChatMessage(msg.Content));
            }
        }
        return messages;
    }
}

