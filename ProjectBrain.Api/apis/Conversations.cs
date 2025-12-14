using ProjectBrain.Api.Authentication;
using ProjectBrain.Api.Exceptions;
using ProjectBrain.Domain;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Shared.Dtos.Pagination;

public class ConversationServices(
    IConversationService conversationService,
    IConversationRepository conversationRepository,
    IIdentityService identityService,
    ILogger<ConversationServices> logger,
    IConfiguration config)
{
    public ILogger<ConversationServices> Logger { get; } = logger;
    public IConfiguration Config { get; } = config;
    public IConversationService ConversationService { get; } = conversationService;
    public IConversationRepository ConversationRepository { get; } = conversationRepository;
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

    private static async Task<IResult> GetConversationWithMessagesById(
        [AsParameters] ConversationServices services,
        Guid id)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();
        var conversation = await services.ConversationService.GetByIdWithMessages(id, userId);
        if (conversation is null)
            return Results.NotFound();

        // Map to DTO to avoid circular reference (Conversation -> Messages -> Conversation)
        var messages = conversation.Messages ?? new List<ChatMessage>();
        var messageDtos = messages.Select(m => new ChatMessageDto(m.Role, m.Content)).ToList();

        var conversationDto = new ConversationWithMessagesDto
        {
            Id = conversation.Id,
            UserId = conversation.UserId,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt,
            Messages = messageDtos
        };

        return Results.Ok(conversationDto);
    }

    private static async Task<IResult> GetAllConversationsForUser(
        [AsParameters] ConversationServices services,
        HttpRequest request)
    {
        var userId = services.IdentityService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new AppException("UNAUTHORIZED", "User is not authenticated", 401);
        }

        // Parse pagination parameters
        var pagedRequest = new PagedRequest();
        if (request.Query.TryGetValue("page", out var pageValue) &&
            int.TryParse(pageValue, out var page) && page > 0)
        {
            pagedRequest.Page = page;
        }

        if (request.Query.TryGetValue("pageSize", out var pageSizeValue) &&
            int.TryParse(pageSizeValue, out var pageSize) && pageSize > 0)
        {
            pagedRequest.PageSize = pageSize;
        }

        // Get total count for pagination
        var totalCount = await services.ConversationRepository.CountAsync(
            c => c.UserId == userId,
            CancellationToken.None);

        // Get paginated results using efficient database-level pagination
        var skip = pagedRequest.GetSkip();
        var take = pagedRequest.GetTake();
        var paginatedConversations = await services.ConversationRepository.GetPagedForUserAsync(userId, skip, take, CancellationToken.None);

        // Map to DTOs (using anonymous objects for now, can create ConversationResponseDto later)
        var conversationDtos = paginatedConversations.Select(c => new
        {
            id = c.Id.ToString(),
            userId = c.UserId,
            title = c.Title,
            createdAt = c.CreatedAt.ToString("O"),
            updatedAt = c.UpdatedAt.ToString("O")
        });

        var response = PagedResponse<object>.Create(pagedRequest, conversationDtos, totalCount);
        return Results.Ok(response);
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

public class ConversationWithMessagesDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ChatMessageDto> Messages { get; set; } = new();
}