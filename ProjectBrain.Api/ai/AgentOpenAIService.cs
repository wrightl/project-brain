namespace ProjectBrain.AI;

using System.Linq;
using System.Text;
using System.Text.Json;
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

    public async Task<AgentSession> BeginSessionAsync(
        AgentSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var aiSettings = await _services.ApplicationSettingsService.GetAISettingsAsync();
        var promptBudgetSettings = await _services.ApplicationSettingsService.GetPromptBudgetSettingsAsync();
        var estimator = await TokenEstimatorFactory.CreateAsync(_services.ApplicationSettingsService);
        var includeOnboarding = ChatPromptAssembler.ShouldIncludeOnboardingBlob(
            aiSettings.IncludeFullOnboardingBlob,
            request.UserInformation,
            request.MemoryContext,
            isFirstTurn: request.History.Count == 0);

        var systemPrompt = BuildAgentSystemPrompt(request.UserName, request.MemoryContext);

        List<AgentChatMessage> limitedHistory;
        string userPrompt;
        IReadOnlyList<PromptSlotTrace> slotTraces = Array.Empty<PromptSlotTrace>();

        if (promptBudgetSettings.EnablePromptBudget)
        {
            var initialHistory = ChatPromptAssembler.SelectRecentAgentHistory(request.History, request.MemoryContext).ToList();
            var budgeted = PromptTokenBudgetAssembler.AssembleForAgent(
                request.UserQuery,
                request.UserInformation,
                request.SourcesFormatted,
                request.CitationCount,
                request.MemoryContext,
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
            limitedHistory = ChatPromptAssembler.SelectRecentAgentHistory(request.History, request.MemoryContext).ToList();
            userPrompt = ChatPromptAssembler.BuildAgentUserPrompt(
                request.UserQuery,
                request.UserInformation,
                request.MemoryContext,
                includeOnboarding,
                request.SourcesFormatted,
                request.CitationCount);
        }

        var estimatedTokens = estimator.EstimateTokens(systemPrompt) + estimator.EstimateTokens(userPrompt);
        foreach (var msg in limitedHistory)
        {
            estimatedTokens += estimator.EstimateTokens(msg.Content);
        }

        var traceEnvelope = ChatTurnTraceBuilder.Build(
            request.CorrelationId ?? "no-trace",
            request.ConversationId ?? Guid.Empty,
            request.UserId,
            request.MemoryContext,
            limitedHistory.Count,
            citationCount: request.CitationCount,
            citationIds: request.CitationIds,
            retrievalMode: request.CitationCount > 0 ? "agent_rag" : "agent",
            estimatedTokens,
            aiSettings.MaxTotalTokens,
            truncatedSources: false,
            slotTraces);
        ChatTurnTraceBuilder.Log(_services.Logger, traceEnvelope, "AgentTrace");

        var messages = ToChatMessages(limitedHistory);
        messages.Insert(0, new SystemChatMessage(systemPrompt));
        messages.Add(new UserChatMessage(userPrompt));

        return new AgentSession
        {
            IsInitialTurn = true,
            CorrelationId = request.CorrelationId,
            ConversationId = request.ConversationId,
            SdkMessageState = messages
        };
    }

    public async IAsyncEnumerable<AgentStreamingUpdate> StreamTurnAsync(
        AgentSession session,
        List<Dictionary<string, object>> tools,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = GetSdkMessages(session);
        var chatTools = BuildChatTools(tools);
        var options = new ChatCompletionOptions
        {
            ToolChoice = chatTools.Count > 0 ? ChatToolChoice.CreateAutoChoice() : ChatToolChoice.CreateNoneChoice()
        };
        foreach (var tool in chatTools)
        {
            options.Tools.Add(tool);
        }

        var chatClient = _services.OpenAIClient.GetChatClient(Constants.CHAT_CLIENT_DEPLOYMENT);
        var response = chatClient.CompleteChatStreamingAsync(messages, options, cancellationToken);

        var toolCallsByIndex = new Dictionary<int, StreamingToolCallAccumulator>();

        await foreach (var update in response.WithCancellation(cancellationToken))
        {
            var domainUpdate = new AgentStreamingUpdate();

            foreach (var choice in update.ContentUpdate)
            {
                if (choice.Text != null)
                {
                    domainUpdate.Text = choice.Text;
                }
            }

            foreach (var toolCallUpdate in update.ToolCallUpdates)
            {
                if (!toolCallsByIndex.TryGetValue(toolCallUpdate.Index, out var accumulator))
                {
                    accumulator = new StreamingToolCallAccumulator();
                    toolCallsByIndex[toolCallUpdate.Index] = accumulator;
                }

                if (!string.IsNullOrEmpty(toolCallUpdate.ToolCallId))
                {
                    accumulator.ToolCallId = toolCallUpdate.ToolCallId;
                }

                if (!string.IsNullOrEmpty(toolCallUpdate.FunctionName))
                {
                    accumulator.FunctionName = toolCallUpdate.FunctionName;
                }

                if (toolCallUpdate.FunctionArgumentsUpdate is { } argumentsUpdate)
                {
                    accumulator.ArgumentsBuilder.Append(argumentsUpdate.ToString());
                }
            }

            if (domainUpdate.Text != null)
            {
                yield return domainUpdate;
            }
        }

        var completedToolCalls = BuildAgentToolCalls(toolCallsByIndex);
        if (completedToolCalls.Count > 0)
        {
            yield return new AgentStreamingUpdate
            {
                ToolCalls = completedToolCalls
            };
        }

        session.IsInitialTurn = false;
    }

    private sealed class StreamingToolCallAccumulator
    {
        public string ToolCallId { get; set; } = string.Empty;
        public string FunctionName { get; set; } = string.Empty;
        public StringBuilder ArgumentsBuilder { get; } = new();
    }

    private static List<AgentToolCall> BuildAgentToolCalls(
        Dictionary<int, StreamingToolCallAccumulator> toolCallsByIndex)
    {
        var result = new List<AgentToolCall>();

        foreach (var (index, accumulator) in toolCallsByIndex.OrderBy(kv => kv.Key))
        {
            if (string.IsNullOrWhiteSpace(accumulator.FunctionName))
            {
                continue;
            }

            var toolCallId = string.IsNullOrWhiteSpace(accumulator.ToolCallId)
                ? $"call_{index}"
                : accumulator.ToolCallId;

            var parameters = new Dictionary<string, object>();
            if (accumulator.ArgumentsBuilder.Length > 0)
            {
                try
                {
                    parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(
                        accumulator.ArgumentsBuilder.ToString()) ?? new Dictionary<string, object>();
                }
                catch (JsonException)
                {
                    // Model may stream malformed JSON; leave parameters empty.
                }
            }

            result.Add(new AgentToolCall
            {
                ToolCallId = toolCallId,
                FunctionName = accumulator.FunctionName,
                Parameters = parameters
            });
        }

        return result;
    }

    public void AppendToolResults(
        AgentSession session,
        string? assistantText,
        IReadOnlyList<AgentToolCall> toolCalls,
        IReadOnlyList<AgentToolResult> toolResults)
    {
        var messages = GetSdkMessages(session);

        var validToolCalls = toolCalls
            .Where(tc => !string.IsNullOrWhiteSpace(tc.FunctionName) && !string.IsNullOrWhiteSpace(tc.ToolCallId))
            .ToList();

        if (validToolCalls.Count > 0)
        {
            var sdkToolCalls = validToolCalls.Select(tc =>
                ChatToolCall.CreateFunctionToolCall(
                    tc.ToolCallId,
                    tc.FunctionName,
                    BinaryData.FromString(JsonSerializer.Serialize(tc.Parameters)))).ToList();

            messages.Add(new AssistantChatMessage(sdkToolCalls));
        }
        else if (!string.IsNullOrWhiteSpace(assistantText))
        {
            messages.Add(new AssistantChatMessage(assistantText));
        }

        foreach (var result in toolResults)
        {
            if (string.IsNullOrWhiteSpace(result.FunctionName) || string.IsNullOrWhiteSpace(result.ToolCallId))
            {
                continue;
            }

            messages.Add((ToolChatMessage)CreateFunctionMessage(
                result.ToolCallId,
                result.FunctionName,
                result.Result));
        }

        session.SdkMessageState = messages;
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
        var session = await BeginSessionAsync(new AgentSessionRequest
        {
            UserQuery = userQuery,
            UserId = userId,
            UserInformation = userInformation,
            UserName = userName,
            History = history,
            MemoryContext = memoryContext,
            ConversationId = conversationId,
            CorrelationId = correlationId
        }, cancellationToken);

        await foreach (var update in StreamTurnAsync(session, tools, cancellationToken))
        {
            yield return update;
        }
    }

    public object CreateFunctionMessage(string toolCallId, string functionName, object result)
    {
        var resultJson = JsonSerializer.Serialize(result);
        return new ToolChatMessage(toolCallId, resultJson);
    }

    private static List<ChatMessage> GetSdkMessages(AgentSession session)
    {
        if (session.SdkMessageState is not List<ChatMessage> messages)
        {
            throw new InvalidOperationException("Agent session has no SDK message state.");
        }

        return messages;
    }

    private static List<ChatTool> BuildChatTools(List<Dictionary<string, object>> tools)
    {
        var chatTools = new List<ChatTool>();
        foreach (var toolDef in tools)
        {
            if (toolDef.TryGetValue("type", out var type) && type?.ToString() == "function"
                && toolDef.TryGetValue("function", out var funcObj) && funcObj is Dictionary<string, object> funcDict)
            {
                var toolName = funcDict["name"]?.ToString() ?? "";
                var toolDescription = funcDict.TryGetValue("description", out var desc) ? desc?.ToString() : null;
                var toolParameters = funcDict.TryGetValue("parameters", out var paramsObj)
                    ? JsonSerializer.Serialize(paramsObj)
                    : "{}";

                chatTools.Add(ChatTool.CreateFunctionTool(
                    toolName,
                    toolDescription,
                    BinaryData.FromString(toolParameters)));
            }
        }

        return chatTools;
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
        prompt.AppendLine("- Plan daily goals for multiple days (up to 7) using create_goals_for_days");
        prompt.AppendLine("- Retrieve today's goals, goal streaks, and incomplete goal backlog");
        prompt.AppendLine("- Suggest daily goals (suggest_daily_goals) before creating them with create_daily_goals");
        prompt.AppendLine("- Mark goals as complete or incomplete");
        prompt.AppendLine("- Suggest, save, rate, and list coping strategies");
        prompt.AppendLine("- Create journal entries and view recent journal history and streaks");
        prompt.AppendLine("- List, remember, and forget user memories (forget requires user confirmation)");
        prompt.AppendLine("- Upload, list, and delete markdown knowledge documents (delete requires user confirmation)");
        prompt.AppendLine("- Search for coaches and view connected coaches");
        prompt.AppendLine("- Answer questions using the user's uploaded knowledge sources when provided");
        prompt.AppendLine("- Help organize and prioritize tasks");
        prompt.AppendLine("- Suggest actions based on conversation context");
        prompt.AppendLine("- Ask the user to choose from options using ask_user when offering 2-6 discrete choices");
        prompt.AppendLine();
        prompt.AppendLine("When offering 2-6 discrete choices, you MUST call ask_user with structured options instead of listing choices as bullets or numbered text in your message. You may still write a brief friendly question in your reply, but the clickable options must be passed to ask_user.");
        prompt.AppendLine();
        prompt.AppendLine("After search_coaches or get_connected_coaches, when offering follow-up choices:");
        prompt.AppendLine("- Use ask_user with an action object on each option so the app can navigate directly.");
        prompt.AppendLine("- For view profile: action { type: \"view_coach_profile\", coachProfileId } from the tool result.");
        prompt.AppendLine("- For message coach: only offer when connectionStatus is \"connected\"; use action { type: \"message_coach\", coachProfileId, connectionId }.");
        prompt.AppendLine("- Do not offer message_coach for coaches who are not connected; offer view_coach_profile instead and explain they must connect first.");
        prompt.AppendLine();
        prompt.AppendLine("When appropriate, proactively offer to:");
        prompt.AppendLine("- Call get_todays_goals or get_incomplete_goal_backlog before overwriting goals");
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

    private static List<ChatMessage> ToChatMessages(List<AgentChatMessage> history)
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
