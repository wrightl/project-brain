using ProjectBrain.Api.Authentication;

public class ConversationServices(
    IConversationService conversationService,
    IIdentityService identityService,
    ILogger<ConversationServices> logger,
    IConfiguration config)
{
    public ILogger<ConversationServices> Logger { get; } = logger;
    public IConfiguration Config { get; } = config;
    public IConversationService ConversationService { get; } = conversationService;
    public IIdentityService IdentityService { get; } = identityService;
}

public static class ConversationEndpoints
{
    public static void MapConversationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("conversation").RequireAuthorization();

        group.MapPost("/", CreateConversation).WithName("CreateConversation");
        group.MapGet("/{id:guid}", GetConversationById).WithName("GetConversationById");
        group.MapGet("/{id:guid}/full", GetConversationWithMessagesById).WithName("GetConversationWithMessagesById");
        group.MapGet("/", GetAllConversationsForUser).WithName("GetAllConversationsForUser");
        group.MapGet("/{conversationId:guid}/messages", GetConversationMessages).WithName("GetConversationMessages");
        group.MapPut("/{id:guid}", UpdateConversation).WithName("UpdateConversation");
        group.MapDelete("/{id:guid}", DeleteConversation).WithName("DeleteConversation");
    }

    private static async Task<IResult> CreateConversation(
        [AsParameters] ConversationServices services,
        CreateConversationRequest request)
    {
        var userId = services.IdentityService.UserId!;

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = request.Title,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        var createdConversation = await services.ConversationService.Add(conversation);
        return Results.Created($"/conversation/{createdConversation.Id}", createdConversation);
    }

    private static async Task<IResult> GetConversationById(
        [AsParameters] ConversationServices services,
        Guid id)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();
        var conversation = await services.ConversationService.GetById(id, userId);
        return conversation is not null ? Results.Ok(conversation) : Results.NotFound();
    }

    // TODO: This method doesn't work
    private static async Task<IResult> GetConversationWithMessagesById(
        [AsParameters] ConversationServices services,
        Guid id)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();
        var conversation = await services.ConversationService.GetByIdWithMessages(id, userId);
        return conversation is not null ? Results.Ok(conversation) : Results.NotFound();
    }

    private static async Task<IResult> GetAllConversationsForUser(
        [AsParameters] ConversationServices services)
    {
        var userId = services.IdentityService.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated");

        var conversations = await services.ConversationService.GetAllForUser(userId);
        return Results.Ok(conversations);
    }

    private async static Task<IResult> GetConversationMessages(
        [AsParameters] ChatServices services,
        Guid conversationId)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            services.Logger.LogWarning("Unauthorized access attempt to get chat messages at {Time}", DateTime.Now);
            return Results.Unauthorized();
        }

        var conversation = await services.ConversationService.GetByIdWithMessages(conversationId, userId);
        if (conversation == null)
        {
            return Results.NotFound();
        }

        var messages = conversation.Messages ?? new List<ChatMessage>();
        var messageDtos = messages.Select(m => new ChatMessageDto(m.Role, m.Content)).ToList();

        return Results.Ok(messageDtos);
    }

    private static async Task<IResult> UpdateConversation(
        [AsParameters] ConversationServices services,
        Guid id,
        UpdateConversationRequest request)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();
        var conversation = await services.ConversationService.GetById(id, userId);
        if (conversation is null)
        {
            return Results.NotFound();
        }

        conversation.Title = request.Title;
        conversation.UpdatedAt = DateTime.Now;

        var updatedConversation = await services.ConversationService.Update(conversation);
        return Results.Ok(updatedConversation);
    }

    private static async Task<IResult> DeleteConversation(
        [AsParameters] ConversationServices services,
        Guid id)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();
        var conversation = await services.ConversationService.GetById(id, userId);
        if (conversation is null)
        {
            return Results.NotFound();
        }

        await services.ConversationService.Remove(conversation);
        return Results.NoContent();
    }
}

public class CreateConversationRequest
{
    public required string Title { get; init; }
}

public class UpdateConversationRequest
{
    public required string Title { get; init; }
}