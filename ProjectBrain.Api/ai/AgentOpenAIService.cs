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
        List<Dictionary<string, object>> tools,
        CancellationToken cancellationToken = default)
    {
        _services.Logger.LogInformation("Starting GetAgentResponseAsync for userQuery: {UserQuery}, userId: {UserId}, userName: {UserName}", userQuery, userId, userName);

        // Build system prompt
        var systemPrompt = BuildAgentSystemPrompt(userName);

        // Limit conversation history
        var maxHistoryMessages = int.Parse(_services.Configuration["AI:MaxHistoryMessages"] ?? "10");
        var limitedHistory = history.TakeLast(maxHistoryMessages).ToList();

        // Create chat messages
        var messages = ToChatMessages(limitedHistory);

        // Add system message
        messages.Insert(0, new SystemChatMessage(systemPrompt));

        // Build user prompt with context
        var userPrompt = BuildAgentUserPrompt(userQuery, userInformation, limitedHistory);
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

    private string BuildAgentSystemPrompt(string userName)
    {
        var prompt = new StringBuilder();

        prompt.AppendLine("You are a proactive AI assistant for neurodiverse individuals. You can perform actions on behalf of users to help them manage their daily goals and tasks.");
        prompt.AppendLine();
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

    private string BuildAgentUserPrompt(string userQuery, string userInformation, List<AgentChatMessage> history)
    {
        var prompt = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(userInformation))
        {
            prompt.AppendLine("---");
            prompt.AppendLine("Here is some data in json format about the user based on their onboarding data:");
            prompt.AppendLine(userInformation);
            prompt.AppendLine("---");
            prompt.AppendLine();
        }

        prompt.AppendLine("User Query:");
        prompt.AppendLine(userQuery);

        if (history.Count > 0)
        {
            prompt.AppendLine();
            prompt.AppendLine("Note: Use the conversation history above for context when answering and deciding what actions to take.");
        }

        return prompt.ToString();
    }
}

