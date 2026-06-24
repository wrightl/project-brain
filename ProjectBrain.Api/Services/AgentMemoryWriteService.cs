using ProjectBrain.Database.Models;
using ProjectBrain.Domain;

namespace ProjectBrain.Api.Services;

public sealed class AgentMemoryWriteService : IAgentMemoryWriteService
{
    private readonly IUserFactService _userFactService;
    private readonly IUserMemoryIndexService _memoryIndexService;

    public AgentMemoryWriteService(
        IUserFactService userFactService,
        IUserMemoryIndexService memoryIndexService)
    {
        _userFactService = userFactService;
        _memoryIndexService = memoryIndexService;
    }

    public async Task<AgentRememberFactResult> RememberFactAsync(
        string userId,
        string content,
        string? category,
        Guid? conversationId,
        CancellationToken cancellationToken = default)
    {
        var trimmed = content.Trim();
        var hash = MemoryHashHelper.ComputeHash(trimmed);

        var existing = await _userFactService.GetByContentHashAsync(userId, hash, cancellationToken);
        if (existing is not null && existing.Status is not MemoryStatuses.Superseded and not MemoryStatuses.Rejected)
        {
            return new AgentRememberFactResult
            {
                Id = existing.Id,
                Content = existing.Content,
                Category = existing.Category
            };
        }

        var fact = new UserFact
        {
            UserId = userId,
            Content = trimmed,
            Category = string.IsNullOrWhiteSpace(category) ? "general" : category.Trim(),
            Status = MemoryStatuses.Active,
            Confidence = 1.0,
            ContentHash = hash,
            SourceConversationId = conversationId,
            ObservationCount = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _userFactService.AddAsync(fact, cancellationToken);
        await _memoryIndexService.IndexFactAsync(created, cancellationToken);

        return new AgentRememberFactResult
        {
            Id = created.Id,
            Content = created.Content,
            Category = created.Category
        };
    }
}
