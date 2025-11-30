using System.Text.Json;
using ProjectBrain.AI;
using _shared = ProjectBrain.Models;
using ProjectBrain.Api.Authentication;
using ProjectBrain.Domain;
using DomainChatService = ProjectBrain.Domain.IChatService;
using DomainConversationService = ProjectBrain.Domain.IConversationService;

public class ChatServices(ILogger<ChatServices> logger,
    IConfiguration config,
    DomainConversationService conversationService,
    DomainChatService chatService,
    AzureOpenAI azureOpenAI,
    Storage storage,
    IIdentityService identityService,
    IUsageTrackingService usageTrackingService,
    IFeatureGateService featureGateService,
    ISubscriptionService subscriptionService)
{
    public ILogger<ChatServices> Logger { get; } = logger;
    public IConfiguration Config { get; } = config;
    public DomainConversationService ConversationService { get; } = conversationService;
    public DomainChatService ChatService { get; } = chatService;
    public AzureOpenAI AzureOpenAI { get; } = azureOpenAI;
    public Storage Storage { get; } = storage;
    public IIdentityService IdentityService { get; } = identityService;
    public IUsageTrackingService UsageTrackingService { get; } = usageTrackingService;
    public IFeatureGateService FeatureGateService { get; } = featureGateService;
    public ISubscriptionService SubscriptionService { get; } = subscriptionService;
}

public static class ChatEndpoints
{

    public static void MapChatEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("chat").RequireAuthorization();

        if (app.Environment.IsDevelopment())
        {
            group.MapGet("/test", GetChatStatus);
        }

        // group.MapPost("/knowledge/upload", UploadKnowledge).WithName("KnowledgeUpload");
        group.MapPost("/voice", StreamVoiceChatEventStream);
        group.MapPost("/stream/json", StreamChatJson);
        group.MapPost("/stream/text", StreamChatPlain);
        group.MapPost("/stream/event-stream", StreamChatEventStream);
        group.MapPost("/stream", StreamChatEventStream);
    }

    private static async Task<object> GetChatStatus([AsParameters] ChatServices services)
    {
        var summary = await services.AzureOpenAI.GetConversationSummary("Hello, how are you?", services.IdentityService.UserId!);
        var endpoint = services.Config["ai:azureOpenAIEndpoint"];
        var key = services.Config["ai:azureOpenAIKey"];
        var deployment = services.Config["ai:azureOpenAIChatDeployment"];
        var searchEndpoint = services.Config["ai:azureSearchEndpoint"]!;
        var searchKey = services.Config["ai:azureSearchApiKey"]!;
        var searchIndexName = services.Config["ai:azureSearchIndexName"] ?? "azureblob-index-chunks";
        var userId = services.IdentityService.UserId;
        var user = await services.IdentityService.GetUserAsync();
        var userName = user?.FullName;
        var email = services.IdentityService.UserEmail;
        return new { status = "ProjectBrain Chat API is running.", summary, endpoint, key, deployment, searchEndpoint, searchKey, searchIndexName, userId, userName, email };
    }

    // private static async Task<IResult> UploadKnowledge([AsParameters] ChatServices services, HttpRequest request)
    // {
    //     var form = await request.ReadFormAsync();

    //     // Get authenticated user from database
    //     var user = await services.IdentityService.GetUserAsync();
    //     if (user == null)
    //         return Results.Unauthorized();

    //     var userId = user.Id;

    //     if (form.Files.Count == 0)
    //         return Results.BadRequest("No files uploaded");

    //     var results = new List<object>();
    //     foreach (var file in form.Files)
    //     {
    //         var filename = form.TryGetValue("filename", out var fn) ? fn.ToString() : file?.FileName;
    //         if (file == null || file.Length == 0)
    //         {
    //             results.Add(new { status = "error", filename, message = "File is empty" });
    //             continue;
    //         }
    //         var chunks = await services.Storage.UploadFile(file, filename!, userId);
    //         results.Add(new { status = "uploaded", filename, chunks });
    //     }

    //     return Results.Ok(results);
    // }

    private static async Task StreamChatJson(
        [AsParameters] ChatServices services,
        ChatRequest request,
        HttpContext http)
    {
        await StreamChat(services, request, http, "application/json");
    }

    private static async Task StreamChatPlain(
        [AsParameters] ChatServices services,
        ChatRequest request,
        HttpContext http)
    {
        await StreamChat(services, request, http, "text/plain");
    }

    private static async Task StreamVoiceChatEventStream(
        [AsParameters] ChatServices services,
        HttpContext http)
    {
        services.Logger.LogInformation("Entering voice chat stream at {0}", DateTime.Now);

        // Get authenticated user from database
        var userId = services.IdentityService.UserId!;

        var isCoach = services.IdentityService.IsCoach;
        var userType = isCoach ? "coach" : "user";

        // Check if speech input is allowed
        var (allowed, errorMessage) = await services.FeatureGateService.CheckFeatureAccessAsync(userId, userType, "speech_input");
        if (!allowed)
        {
            services.Logger.LogWarning("Speech input not allowed for user {UserId}: {ErrorMessage}", userId, errorMessage);
            http.Response.StatusCode = 403; // Forbidden
            http.Response.ContentType = "application/json";
            await http.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { error = errorMessage }));
            return;
        }

        var form = await http.Request.ReadFormAsync();

        // Get conversation ID from form data
        var conversationId = form.TryGetValue("conversationId", out var convId) ? convId.ToString() : null;

        // Get the audio file
        if (form.Files.Count == 0)
        {
            services.Logger.LogWarning("No audio file provided in voice chat request at {Time}", DateTime.Now);
            http.Response.StatusCode = 400; // Bad Request
            await http.Response.WriteAsync("No audio file provided");
            return;
        }

        var audioFile = form.Files[0];
        if (audioFile.Length == 0)
        {
            services.Logger.LogWarning("Empty audio file provided in voice chat request at {Time}", DateTime.Now);
            http.Response.StatusCode = 400; // Bad Request
            await http.Response.WriteAsync("Empty audio file provided");
            return;
        }

        try
        {
            // Transcribe the audio
            string transcribedText;
            using (var audioStream = audioFile.OpenReadStream())
            {
                transcribedText = await services.AzureOpenAI.TranscribeAudio(audioStream, audioFile.FileName ?? "audio.wav");
            }

            if (string.IsNullOrWhiteSpace(transcribedText))
            {
                services.Logger.LogWarning("Audio transcription resulted in empty text at {Time}", DateTime.Now);
                http.Response.StatusCode = 400; // Bad Request
                await http.Response.WriteAsync("Could not transcribe audio");
                return;
            }

            services.Logger.LogInformation("Transcribed audio to text: {TranscribedText}", transcribedText);

            // Create a ChatRequest with the transcribed text
            var chatRequest = new ChatRequest
            {
                ConversationId = conversationId,
                Content = transcribedText,
                IsVoice = true
            };

            // Process the chat request using existing logic
            await StreamChat(services, chatRequest, http, "text/event-stream");
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error processing voice chat request at {Time}", DateTime.Now);
            http.Response.StatusCode = 500; // Internal Server Error
            await http.Response.WriteAsync("Error processing voice request");
        }
    }

    private static async Task StreamChatEventStream(
        [AsParameters] ChatServices services,
        ChatRequest request,
        HttpContext http)
    {
        await StreamChat(services, request, http, "text/event-stream");
    }

    private static async Task StreamChat(
        [AsParameters] ChatServices services,
        ChatRequest request,
        HttpContext http,
        string contentType = "text/plain")
    {
        services.Logger.LogInformation("Entering chat stream at {0}", DateTime.Now);

        // Get authenticated user from database
        var userId = services.IdentityService.UserId;
        var user = await services.IdentityService.GetUserAsync();

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            services.Logger.LogWarning("Bad request with empty content at {Time}", DateTime.Now);
            http.Response.StatusCode = 400; // Bad Request
            return;
        }
        else if (request.Content.Length > 2000)
        {
            services.Logger.LogWarning("Bad request with content length {Length} exceeding limit at {Time}", request.Content.Length, DateTime.Now);
            http.Response.StatusCode = 413; // Payload Too Large
            return;
        }
#if DEBUG
        else if (request.Content.Equals("Hello", StringComparison.OrdinalIgnoreCase))
        {
            services.Logger.LogInformation("Received test message 'Hello' at {Time}", DateTime.Now);
            Thread.Sleep(2000); // Simulate processing delay
            http.Response.ContentType = contentType;
            http.Response.StatusCode = 200;
            await http.Response.WriteAsync(new ChatMessageResponseChunk($"Hello! How can I assist you today {user.FirstName}?").ToResponse(contentType));
            return;
        }
#endif

        var isCoach = services.IdentityService.IsCoach;
        var userType = isCoach ? "coach" : "user";

        // Check AI query limits (only for users, not coaches)
        if (userType == "user")
        {
            // Check daily limit
            var dailyLimit = int.Parse(services.Config["TierLimits:User:Free:DailyAIQueries"] ?? "50");
            var dailyUsage = await services.UsageTrackingService.GetUsageCountAsync(userId, "ai_query", "daily");
            var tier = await services.SubscriptionService.GetUserTierAsync(userId, userType);
            var tierDailyLimit = int.Parse(services.Config[$"TierLimits:User:{tier}:DailyAIQueries"] ?? "-1");
            var effectiveDailyLimit = tierDailyLimit >= 0 ? tierDailyLimit : dailyLimit;

            if (effectiveDailyLimit >= 0 && dailyUsage >= effectiveDailyLimit)
            {
                services.Logger.LogWarning("Daily AI query limit reached for user {UserId}: {Usage}/{Limit}", userId, dailyUsage, effectiveDailyLimit);
                http.Response.StatusCode = 429; // Too Many Requests
                http.Response.ContentType = "application/json";
                await http.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
                {
                    error = $"You have reached your daily limit of {effectiveDailyLimit} AI queries. Please upgrade or try again tomorrow."
                }));
                return;
            }

            // Check monthly limit
            var monthlyLimit = int.Parse(services.Config["TierLimits:User:Free:MonthlyAIQueries"] ?? "200");
            var monthlyUsage = await services.UsageTrackingService.GetUsageCountAsync(userId, "ai_query", "monthly");
            var tierMonthlyLimit = int.Parse(services.Config[$"TierLimits:User:{tier}:MonthlyAIQueries"] ?? "-1");
            var effectiveMonthlyLimit = tierMonthlyLimit >= 0 ? tierMonthlyLimit : monthlyLimit;

            if (effectiveMonthlyLimit >= 0 && monthlyUsage >= effectiveMonthlyLimit)
            {
                services.Logger.LogWarning("Monthly AI query limit reached for user {UserId}: {Usage}/{Limit}", userId, monthlyUsage, effectiveMonthlyLimit);
                http.Response.StatusCode = 429; // Too Many Requests
                http.Response.ContentType = "application/json";
                await http.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
                {
                    error = $"You have reached your monthly limit of {effectiveMonthlyLimit} AI queries. Please upgrade for unlimited queries."
                }));
                return;
            }
        }

        // Get/Create Conversation
        Conversation? conversation;

        if (request.ConversationId is null)
        {
            var chatSummaryResponse = await services.AzureOpenAI.GetConversationSummary(request.Content, userId);

            conversation = await services.ConversationService.Add(new Conversation
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = chatSummaryResponse,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            conversation = await services.ConversationService.GetByIdWithMessages(Guid.Parse(request.ConversationId!), userId);

            if (conversation is null)
            {
                services.Logger.LogError("Failed to retrieve or create conversation for user {UserId} at {Time}", userId, DateTime.Now);
                http.Response.StatusCode = 400;
                return;
            }
            else if (conversation.UserId != userId)
            {
                services.Logger.LogWarning("Forbidden access attempt to conversation {ConversationId} by user {UserId} at {Time}", conversation.Id, userId, DateTime.Now);
                http.Response.StatusCode = 403; // Forbidden
                return;
            }
        }

        http.Response.ContentType = contentType;
        http.Response.StatusCode = 200;
        http.Response.Headers["X-Conversation-Id"] = conversation.Id.ToString();

        List<_shared.ChatMessage> history = conversation.Messages.Select(m => new _shared.ChatMessage
        {
            Role = m.Role == "user" ? _shared.ChatMessageRole.User : _shared.ChatMessageRole.Assistant,
            Content = m.Content
        }).ToList();

        services.Logger.LogInformation("Chat history for conversation {ConversationId}: {History}", conversation.Id, JsonSerializer.Serialize(history));

        var userName = user.FirstName!;
        services.Logger.LogInformation("Using user name {UserName} for conversation {ConversationId}", userName, conversation.Id);

        var (chatResponse, citations) = await services.AzureOpenAI.GetResponseWithCitations(request.Content, userId, userName, history);

        services.Logger.LogInformation("Citations: {Citations}", JsonSerializer.Serialize(citations));
        // Send citations as metadata before streaming the response
        if (citations.Any())
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

            if (contentType == "text/event-stream")
            {
                await http.Response.WriteAsync($"data: {JsonSerializer.Serialize(new { type = "citations", value = citationsData })}\n\n");
            }
            else if (contentType == "application/json")
            {
                // For JSON, we could include citations in a separate field
                // For now, we'll send it as a separate message type
                await http.Response.WriteAsync(JsonSerializer.Serialize(new { type = "citations", value = citationsData }) + "\n");
            }
            await http.Response.Body.FlushAsync();
        }

        var assistantMessages = new List<string>();

        await foreach (var line in chatResponse)
        {
            foreach (var choice in line.ContentUpdate)
            {
                if (choice.Text != null)
                {
                    assistantMessages.Add(choice.Text);
                    services.Logger.LogInformation("Streaming chunk: {Chunk}", choice.Text);
                    await http.Response.WriteAsync(new ChatMessageResponseChunk(choice.Text).ToResponse(contentType));
                    await http.Response.Body.FlushAsync();
                }
            }
        }

        var chatMessage = await services.ChatService.Add(new ChatMessage
        {
            ConversationId = conversation.Id,
            Role = "user",
            Content = request.Content,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await services.ChatService.Add(new ChatMessage
        {
            ConversationId = conversation.Id,
            Role = "assistant",
            Content = string.Join("", assistantMessages),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        // Track AI query usage (only for users)
        if (userType == "user")
        {
            await services.UsageTrackingService.TrackAIQueryAsync(userId);
        }
    }
}

public class ChatRequest
{
    public string? ConversationId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsVoice { get; set; } = false;
}

public class ChatMessageResponseChunk(string Value, string Type = "text")
{
    public string ToResponse(string contentType)
    {
        if (contentType == "text/plain")
        {
            return $"{Value}\n";
        }
        else if (contentType == "text/event-stream")
        {
            return $"data: {JsonSerializer.Serialize(new { type = Type, value = Value })}\n\n";
        }
        else
        {
            return JsonSerializer.Serialize(new { type = Type, value = Value });
        }
    }
}

public record ChatMessageDto(string Role, string Content);