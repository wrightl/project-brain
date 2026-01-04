using System.Linq;
using System.Text.Json;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain;
using DomainConversationService = ProjectBrain.Domain.IConversationService;
using DomainChatService = ProjectBrain.Domain.IChatService;
using ProjectBrain.Domain.Dtos;
using Microsoft.Extensions.DependencyInjection;

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
    IAgentOrchestrator orchestrator)
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
            var chatSummaryResponse = await services.Storage.GetFile(Constants.ONBOARDING_DATA_FILENAME, new StorageOptions
            {
                UserId = userId,
                FileOwnership = FileOwnership.User,
                StorageType = StorageType.Onboarding
            }) != null
                ? "Agent interaction"
                : "Agent interaction";

            conversation = await services.ConversationService.Add(new Conversation
            {
                Id = Guid.NewGuid(),
                UserId = userId!,
                Title = chatSummaryResponse,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
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

        // Get the onboarding data for the user
        string userInformation = string.Empty;
        var options = new StorageOptions { UserId = userId, FileOwnership = FileOwnership.User, StorageType = StorageType.Onboarding };
        var userInformationStream = await services.Storage.GetFile(Constants.ONBOARDING_DATA_FILENAME, options);
        if (userInformationStream is not null)
        {
            using (var reader = new StreamReader(userInformationStream))
            {
                userInformation = await reader.ReadToEndAsync();
            }
        }

        // Process agent interaction
        var agentResponse = await services.AgentService.ProcessAgentInteractionAsync(
            userId!,
            request.Content,
            conversation.Id,
            request.WorkflowId,
            userInformation,
            userName,
            history,
            http.RequestAborted);

        // Send workflow ID
        if (agentResponse.WorkflowId.HasValue)
        {
            await http.Response.WriteAsync($"data: {JsonSerializer.Serialize(new { type = "workflow", value = new { id = agentResponse.WorkflowId.Value } })}\n\n");
            await http.Response.Body.FlushAsync();
        }

        // Send assistant message as text chunks (if available)
        if (!string.IsNullOrEmpty(agentResponse.Message))
        {
            // Send the message as text chunks to simulate streaming
            var message = agentResponse.Message;
            await http.Response.WriteAsync($"data: {JsonSerializer.Serialize(new { type = "text", value = message })}\n\n");
            await http.Response.Body.FlushAsync();
        }

        // Send tool execution results
        if (agentResponse.ExecutedTools.Any())
        {
            var toolExecutions = agentResponse.ExecutedTools.Select(t => new
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
        }

        // Send final status
        await http.Response.WriteAsync($"data: {JsonSerializer.Serialize(new { type = "status", value = new { status = agentResponse.Status, error = agentResponse.ErrorMessage } })}\n\n");
        await http.Response.Body.FlushAsync();

        // Save messages to conversation
        await services.ChatService.Add(new ChatMessage
        {
            ConversationId = conversation.Id,
            Role = "user",
            Content = request.Content,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var assistantMessage = $"Agent processed your request. Status: {agentResponse.Status}";
        if (agentResponse.ExecutedTools.Any())
        {
            assistantMessage += $". Executed {agentResponse.ExecutedTools.Count} tool(s).";
        }

        await services.ChatService.Add(new ChatMessage
        {
            ConversationId = conversation.Id,
            Role = "assistant",
            Content = assistantMessage,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await services.UsageTrackingService.TrackAIQueryAsync(userId!);
    }

    private static async Task<IResult> GetAvailableTools([AsParameters] AgentServices services)
    {
        var tools = services.AgentService.GetAvailableTools();
        return Results.Ok(tools);
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

        return true;
    }
}

public class AgentRequest
{
    public string? ConversationId { get; set; }
    public Guid? WorkflowId { get; set; }
    public string Content { get; set; } = string.Empty;
}

