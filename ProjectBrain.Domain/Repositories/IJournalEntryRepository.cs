namespace ProjectBrain.Domain.Repositories;

/// <summary>
/// Repository interface for JournalEntry entity with domain-specific queries
/// </summary>
public interface IJournalEntryRepository : IRepository<JournalEntry, Guid>
{
    /// <summary>
    /// Gets a journal entry by ID for a specific user
    /// </summary>
    Task<JournalEntry?> GetByIdForUserAsync(Guid id, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a journal entry by ID with tags for a specific user
    /// </summary>
    Task<JournalEntry?> GetByIdWithTagsForUserAsync(Guid id, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all journal entries for a user ordered by created date
    /// </summary>
    Task<IEnumerable<JournalEntry>> GetAllForUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated journal entries for a user (efficient database-level pagination)
    /// </summary>
    Task<IEnumerable<JournalEntry>> GetPagedForUserAsync(string userId, int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent journal entries for a user
    /// </summary>
    Task<IEnumerable<JournalEntry>> GetRecentForUserAsync(string userId, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts journal entries for a user
    /// </summary>
    Task<int> CountForUserAsync(string userId, CancellationToken cancellationToken = default);
}

