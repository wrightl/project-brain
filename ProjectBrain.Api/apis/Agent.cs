using System.Linq;
using System.Text;
using System.Text.Json;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Api.Background;
using ProjectBrain.Api.Services;
using ProjectBrain.Domain;
using DomainConversationService = ProjectBrain.Domain.IConversationService;
using DomainChatService = ProjectBrain.Domain.IChatService;
using ProjectBrain.Domain.Dtos;
using Microsoft.Extensions.DependencyInjection;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces.Managers;

public class AgentServices(
    ILogger<AgentServices> logger,
    IConfiguration config,
    DomainConversationService conversationService,
    DomainChatService chatService,
    IAgentService agentService,
    Storage storage,
    IIdentityService identityService,
    IUsageTrackingService usageTrackingService,
    IFeatureGateService featureGateService,
    ISubscriptionService subscriptionService,
    IAgentOrchestrator orchestrator,
    IChatMemoryContextService chatMemoryContextService,
    IChatPersistenceQueue chatPersistenceQueue,
    ITimeTickerManager<TimeTickerEntity> timeTickerManager)
{
    public ILogger<AgentServices> Logger { get; } = logger;
    public IConfiguration Config { get; } = config;
    public DomainConversationService ConversationService { get; } = conversationService;
    public DomainChatService ChatService { get; } = chatService;
    public IAgentService AgentService { get; } = agentService;
    public Storage Storage { get; } = storage;
    public IIdentityService IdentityService { get; } = identityService;
    public IUsageTrackingService UsageTrackingService { get; } = usageTrackingService;
    public IFeatureGateService FeatureGateService { get; } = featureGateService;
    public ISubscriptionService SubscriptionService { get; } = subscriptionService;
    public IAgentOrchestrator Orchestrator { get; } = orchestrator;
    public IChatMemoryContextService ChatMemoryContextService { get; } = chatMemoryContextService;
    public IChatPersistenceQueue ChatPersistenceQueue { get; } = chatPersistenceQueue;
    public ITimeTickerManager<TimeTickerEntity> TimeTickerManager { get; } = timeTickerManager;
}

public static class AgentEndpoints
{
    public static void MapAgentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("agent").RequireAuthorization("UserOnly");

        group.MapPost("/stream", StreamAgentEventStream).WithName("StreamAgent");
        group.MapPost("/stream/event-stream", StreamAgentEventStream).WithName("StreamAgentEventStream");
        group.MapGet("/tools", GetAvailableTools).WithName("GetAvailableTools");
        group.MapGet("/workflows", GetWorkflows).WithName("GetWorkflows");
        group.MapPost("/workflows/{workflowId:guid}/actions/{actionId:guid}/confirm", ConfirmPendingAction).WithName("ConfirmAgentPendingAction");
        group.MapPost("/workflows/{workflowId:guid}/actions/{actionId:guid}/cancel", CancelPendingAction).WithName("CancelAgentPendingAction");
        group.MapPost("/workflows/{id}/resume", ResumeWorkflow).WithName("ResumeWorkflow");
        group.MapPost("/workflows/{id}/cancel", CancelWorkflow).WithName("CancelWorkflow");
    }

    private static async Task StreamAgentEventStream(
        [AsParameters] AgentServices services,
        AgentRequest request,
        HttpContext http)
    {
        services.Logger.LogInformation("Entering agent stream at {0}", DateTime.Now);

        // Check feature flag (defense-in-depth)
        var featureFlagService = http.RequestServices.GetRequiredService<IFeatureFlagService>();
        var agentFeatureEnabled = await featureFlagService.IsFeatureEnabled(FeatureFlags.AgentFeatureEnabled);
        if (!agentFeatureEnabled)
        {
            services.Logger.LogWarning("Agent feature is disabled via feature flag for user {UserId}", services.IdentityService.UserId);
            http.Response.StatusCode = 403; // Forbidden
            await http.Response.WriteAsync("Agent feature is currently disabled.");
            return;
        }

        var userId = services.IdentityService.UserId;
        var user = await services.IdentityService.GetUserAsync();

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            services.Logger.LogWarning("Bad request with empty content at {Time}", DateTime.Now);
            http.Response.StatusCode = 400;
            return;
        }

        if (request.Content.Length > 2000)
        {
            services.Logger.LogWarning("Bad request with content length {Length} exceeding limit at {Time}", request.Content.Length, DateTime.Now);
            http.Response.StatusCode = 413;
            return;
        }

        // Check usage limits
        if (!await CheckUsageLimits(services, http, userId))
        {
            return;
        }

        // Get/Create Conversation
        Conversation? conversation;
        if (request.ConversationId is null)
        {
            var placeholderTitle = ConversationTitleHelper.BuildPlaceholderTitle(request.Content);
            conversation = await services.ConversationService.Add(new Conversation
            {
                Id = Guid.NewGuid(),
                UserId = userId!,
                Title = placeholderTitle,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            try
            {
                await UserContextTickerEnqueue.EnqueueConversationTitleSummaryAsync(
                    services.TimeTickerManager,
                    userId!,
                    conversation.Id,
                    request.Content,
                    http.RequestAborted);
            }
            catch (Exception ex)
            {
                services.Logger.LogWarning(ex, "Failed to enqueue conversation title summary for {ConversationId}", conversation.Id);
            }
        }
        else
        {
            conversation = await services.ConversationService.GetByIdWithMessages(Guid.Parse(request.ConversationId!), userId!);
            if (conversation is null || conversation.UserId != userId)
            {
                services.Logger.LogError("Failed to retrieve conversation for user {UserId} at {Time}", userId, DateTime.Now);
                http.Response.StatusCode = conversation == null ? 404 : 403;
                return;
            }
        }

        http.Response.ContentType = "text/event-stream";
        http.Response.StatusCode = 200;
        http.Response.Headers["X-Conversation-Id"] = conversation.Id.ToString();

        // Convert to domain DTOs
        List<AgentChatMessage> history = conversation.Messages.Select(m => new AgentChatMessage
        {
            Role = m.Role == "user" ? AgentChatMessageRole.User : AgentChatMessageRole.Assistant,
            Content = m.Content
        }).ToList();

        var userName = user?.FirstName ?? "User";
        services.Logger.LogInformation("Using user name {UserName} for agent conversation {ConversationId}", userName, conversation.Id);

        var memoryContext = await services.ChatMemoryContextService.BuildAsync(
            userId!,
            conversation.Id,
            request.Content,
            http.RequestAborted);

        // Get the onboarding data for the user
        string userInformation = string.Empty;
        var options = new StorageOptions { UserId = userId, FileOwnership = FileOwnership.User, StorageType = StorageType.Onboarding };
        var userInformationStream = await services.Storage.GetFile(Constants.ONBOARDING_MARKDOWN_FILENAME, options);
        if (userInformationStream is not null)
        {
            using (var reader = new StreamReader(userInformationStream))
            {
                userInformation = await reader.ReadToEndAsync();
            }
        }

        // Stream agent events incrementally
        var assistantContentBuilder = new StringBuilder();
        var allToolExecutions = new List<ToolExecutionRecord>();

        await foreach (var streamEvent in services.AgentService.StreamAgentInteractionAsync(
            userId!,
            request.Content,
            conversation.Id,
            request.WorkflowId,
            userInformation,
            userName,
            history,
            memoryContext,
            UserType.User,
            http.RequestAborted))
        {
            switch (streamEvent.Type)
            {
                case "citations":
                    if (streamEvent.Value is List<ChatCitationDto> citations)
                    {
                        var citationsData = citations.Select(c => new
                        {
                            id = c.Id,
                            index = c.Index,
                            sourceFile = c.SourceFile,
                            sourcePage = c.SourcePage,
                            storageUrl = c.StorageUrl,
                            isShared = c.IsShared
                        }).ToList();

                        await http.Response.WriteAsync($"data: {JsonSerializer.Serialize(new { type = "citations", value = citationsData })}\n\n");
                        await http.Response.Body.FlushAsync();
                    }

                    break;

                case "workflow":
                    await http.Response.WriteAsync($"data: {JsonSerializer.Serialize(new { type = streamEvent.Type, value = streamEvent.Value })}\n\n");
                    await http.Response.Body.FlushAsync();
                    break;

                case "text":
                    if (streamEvent.Value?.ToString() is { Length: > 0 } textChunk)
                    {
                        assistantContentBuilder.Append(textChunk);
                        await http.Response.WriteAsync($"data: {JsonSerializer.Serialize(new { type = "text", value = textChunk })}\n\n");
                        await http.Response.Body.FlushAsync();
                    }

                    break;

                case "tools_executed":
                    if (streamEvent.Value is List<ToolExecutionRecord> tools)
                    {
                        allToolExecutions.AddRange(tools);
                        var toolExecutions = tools.Select(t => new
                        {
                            toolName = t.ToolName,
                            parameters = t.Parameters ?? new Dictionary<string, object>(),
                            result = t.Result,
                            success = t.Success,
                            errorMessage = t.ErrorMessage,
                            executedAt = t.ExecutedAt.ToString("O")
                        }).ToArray();

                        await http.Response.WriteAsync($"data: {JsonSerializer.Serialize(new { type = "tools_executed", value = toolExecutions })}\n\n");
                        await http.Response.Body.FlushAsync();

                        foreach (var tool in tools)
                        {
                            foreach (var card in AgentActionCardMapper.MapToolResult(tool.ToolName, tool.Result, tool.Success))
                            {
                                await http.Response.WriteAsync($"data: {JsonSerializer.Serialize(new { type = "action_card", value = card })}\n\n");
                                await http.Response.Body.FlushAsync();
                            }
                        }
                    }

                    break;

                case "strategies":
                    await http.Response.WriteAsync($"data: {JsonSerializer.Serialize(new { type = "strategies", value = streamEvent.Value })}\n\n");
                    await http.Response.Body.FlushAsync();
                    break;

                case "pending_action":
                    await http.Response.WriteAsync($"data: {JsonSerializer.Serialize(new { type = "pending_action", value = streamEvent.Value })}\n\n");
                    await http.Response.Body.FlushAsync();
                    break;

                case "user_choices":
                    await http.Response.WriteAsync($"data: {JsonSerializer.Serialize(new { type = "user_choices", value = streamEvent.Value })}\n\n");
                    await http.Response.Body.FlushAsync();
                    break;

                case "status":
                    await http.Response.WriteAsync($"data: {JsonSerializer.Serialize(new { type = "status", value = streamEvent.Value })}\n\n");
                    await http.Response.Body.FlushAsync();
                    break;
            }
        }

        var assistantContent = assistantContentBuilder.Length > 0
            ? assistantContentBuilder.ToString()
            : allToolExecutions.Count > 0
                ? "Agent completed your requested actions."
                : "Agent processed your request.";

        await ChatPersistenceHelper.EnqueueOrPersistAsync(
            services.ChatPersistenceQueue,
            services.ChatService,
            services.UsageTrackingService,
            services.TimeTickerManager,
            conversation.Id,
            userId!,
            request.Content,
            assistantContent,
            http.RequestAborted);
    }

    private static async Task<IResult> GetAvailableTools([AsParameters] AgentServices services)
    {
        var userId = services.IdentityService.UserId!;
        var tools = await services.AgentService.GetEnabledToolsAsync(userId);
        return Results.Ok(tools);
    }

    private static async Task<IResult> ConfirmPendingAction(
        [AsParameters] AgentServices services,
        Guid workflowId,
        Guid actionId)
    {
        var userId = services.IdentityService.UserId!;
        var result = await services.AgentService.ConfirmPendingActionAsync(userId, workflowId, actionId);

        if (!result.Success)
        {
            return Results.BadRequest(new { success = false, message = result.Message });
        }

        var response = new
        {
            success = true,
            message = result.Message,
            toolExecution = result.ToolExecution is null ? null : new
            {
                toolName = result.ToolExecution.ToolName,
                parameters = result.ToolExecution.Parameters,
                result = result.ToolExecution.Result,
                success = result.ToolExecution.Success,
                executedAt = result.ToolExecution.ExecutedAt.ToString("O")
            },
            actionCards = result.ToolExecution is null
                ? Array.Empty<object>()
                : AgentActionCardMapper.MapToolResult(
                    result.ToolExecution.ToolName,
                    result.ToolExecution.Result,
                    result.ToolExecution.Success).ToArray()
        };

        return Results.Ok(response);
    }

    private static async Task<IResult> CancelPendingAction(
        [AsParameters] AgentServices services,
        Guid workflowId,
        Guid actionId)
    {
        var userId = services.IdentityService.UserId!;
        var result = await services.AgentService.CancelPendingActionAsync(userId, workflowId, actionId);

        if (!result.Success)
        {
            return Results.BadRequest(new { success = false, message = result.Message });
        }

        return Results.Ok(new { success = true, message = result.Message });
    }

    private static async Task<IResult> GetWorkflows([AsParameters] AgentServices services)
    {
        var userId = services.IdentityService.UserId!;
        var workflows = await services.Orchestrator.GetActiveWorkflowsAsync(userId);
        return Results.Ok(workflows);
    }

    private static async Task<IResult> ResumeWorkflow(
        [AsParameters] AgentServices services,
        Guid id)
    {
        var userId = services.IdentityService.UserId!;
        var workflow = await services.Orchestrator.ResumeWorkflowAsync(id, userId);
        if (workflow == null)
        {
            return Results.NotFound();
        }
        return Results.Ok(workflow);
    }

    private static async Task<IResult> CancelWorkflow(
        [AsParameters] AgentServices services,
        Guid id)
    {
        var userId = services.IdentityService.UserId!;
        var workflow = await services.Orchestrator.LoadWorkflowAsync(id, userId);
        if (workflow == null)
        {
            return Results.NotFound();
        }
        await services.Orchestrator.FailWorkflowAsync(workflow, "Cancelled by user", CancellationToken.None);
        return Results.Ok(new { message = "Workflow cancelled" });
    }

    private static async Task<bool> CheckUsageLimits(AgentServices services, HttpContext http, string? userId)
    {
        var dailyLimit = int.Parse(services.Config["TierLimits:User:Free:DailyAIQueries"] ?? "50");
        var dailyUsage = await services.UsageTrackingService.GetUsageCountAsync(userId, "ai_query", "daily");
        var tier = await services.SubscriptionService.GetUserTierAsync(userId, UserType.User);
        var tierDailyLimit = int.Parse(services.Config[$"TierLimits:User:{tier}:DailyAIQueries"] ?? "-1");
        var effectiveDailyLimit = tierDailyLimit >= 0 ? tierDailyLimit : dailyLimit;

        if (effectiveDailyLimit >= 0 && dailyUsage >= effectiveDailyLimit)
        {
            services.Logger.LogWarning("Daily AI query limit reached for user {UserId}: {Usage}/{Limit}", userId, dailyUsage, effectiveDailyLimit);
            http.Response.StatusCode = 429;
            http.Response.ContentType = "application/json";
            await http.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = $"You have reached your daily limit of {effectiveDailyLimit} AI queries. Please upgrade or try again tomorrow."
            }));
            return false;
        }

        // Match Chat.CheckUsageLimits: monthly caps must apply to agent turns too.
        var monthlyLimit = int.Parse(services.Config["TierLimits:User:Free:MonthlyAIQueries"] ?? "200");
        var monthlyUsage = await services.UsageTrackingService.GetUsageCountAsync(userId, "ai_query", "monthly");
        var tierMonthlyLimit = int.Parse(services.Config[$"TierLimits:User:{tier}:MonthlyAIQueries"] ?? "-1");
        var effectiveMonthlyLimit = tierMonthlyLimit >= 0 ? tierMonthlyLimit : monthlyLimit;

        if (effectiveMonthlyLimit >= 0 && monthlyUsage >= effectiveMonthlyLimit)
        {
            services.Logger.LogWarning("Monthly AI query limit reached for user {UserId}: {Usage}/{Limit}", userId, monthlyUsage, effectiveMonthlyLimit);
            http.Response.StatusCode = 429;
            http.Response.ContentType = "application/json";
            await http.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = $"You have reached your monthly limit of {effectiveMonthlyLimit} AI queries. Please upgrade for unlimited queries."
            }));
            return false;
        }

        return true;
    }
}

public class AgentRequest
{
    public string? ConversationId { get; set; }
    public Guid? WorkflowId { get; set; }
    public string Content { get; set; } = string.Empty;
}

