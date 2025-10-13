using System.Text.Json;
using ProjectBrain.AI;
using _shared = ProjectBrain.Models;
using Microsoft.AspNetCore.Mvc;
using ProjectBrain.Api.Authentication;
using System.Threading.Tasks;

public class ChatServices(ILogger<ChatServices> logger,
    IConfiguration config,
    IConversationService conversationService,
    IChatService chatService,
    AzureOpenAI azureOpenAI,
    IIdentityService identityService)
{
    public ILogger<ChatServices> Logger { get; } = logger;
    public IConfiguration Config { get; } = config;
    public IConversationService ConversationService { get; } = conversationService;
    public IChatService ChatService { get; } = chatService;
    public AzureOpenAI AzureOpenAI { get; } = azureOpenAI;
    public IIdentityService IdentityService { get; } = identityService;
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

        group.MapPost("/knowledge/upload", UploadKnowledge).WithName("KnowledgeUpload");
        group.MapPost("/stream/json", StreamChatJson);
        group.MapPost("/stream/text", StreamChatPlain);
        group.MapPost("/stream/event-stream", StreamChatEventStream);
        group.MapPost("/stream", StreamChatEventStream);
    }

    private static async Task<object> GetChatStatus([AsParameters] ChatServices services)
    {
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
        return new { status = "ProjectBrain Chat API is running.", endpoint, key, deployment, searchEndpoint, searchKey, searchIndexName, userId, userName, email };
    }

    private static async Task<IResult> UploadKnowledge([AsParameters] ChatServices services, HttpRequest request)
    {
        var form = await request.ReadFormAsync();

        // Get authenticated user from database
        var user = await services.IdentityService.GetUserAsync();
        if (user == null)
            return Results.Unauthorized();

        var userId = user.Id;

        if (form.Files.Count == 0)
            return Results.BadRequest("No files uploaded");

        var results = new List<object>();
        foreach (var file in form.Files)
        {
            var filename = form.TryGetValue("filename", out var fn) ? fn.ToString() : file?.FileName;
            if (file == null || file.Length == 0)
            {
                results.Add(new { status = "error", filename, message = "File is empty" });
                continue;
            }
            var chunks = await services.AzureOpenAI.UploadFile(file, userId, filename!);
            results.Add(new { status = "uploaded", filename, chunks });
        }

        return Results.Ok(results);
    }

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
        var user = await services.IdentityService.GetUserAsync();
        if (user == null)
        {
            services.Logger.LogWarning("Unauthorized access attempt to chat stream at {Time}", DateTime.Now);
            http.Response.StatusCode = 401; // Unauthorized
            return;
        }

        var userId = user.Id;

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

        var chatResponse = await services.AzureOpenAI.GetResponse(request.Content, userId, userName, history);

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
    }
}

public class ChatRequest
{
    public string? ConversationId { get; set; }
    public string Content { get; set; } = string.Empty;
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

record ChatMessageDto(string Role, string Content);