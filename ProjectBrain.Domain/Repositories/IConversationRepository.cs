namespace ProjectBrain.Domain.Repositories;

/// <summary>
/// Repository interface for Conversation entity with domain-specific queries
/// </summary>
public interface IConversationRepository : IRepository<Conversation, Guid>
{
    /// <summary>
    /// Gets a conversation by ID for a specific user
    /// </summary>
    Task<Conversation?> GetByIdForUserAsync(Guid id, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a conversation by ID with messages for a specific user
    /// </summary>
    Task<Conversation?> GetByIdWithMessagesForUserAsync(Guid id, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all conversations for a user ordered by updated date
    /// </summary>
    Task<IEnumerable<Conversation>> GetAllForUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated conversations for a user (efficient database-level pagination)
    /// </summary>
    Task<IEnumerable<Conversation>> GetPagedForUserAsync(string userId, int skip, int take, CancellationToken cancellationToken = default);
}

