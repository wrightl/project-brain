namespace ProjectBrain.Domain.Repositories;

/// <summary>
/// Repository interface for VoiceNote entity with domain-specific queries
/// </summary>
public interface IVoiceNoteRepository : IRepository<VoiceNote, Guid>
{
    /// <summary>
    /// Gets a voice note by ID for a specific user
    /// </summary>
    Task<VoiceNote?> GetByIdForUserAsync(Guid id, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all voice notes for a user with optional limit
    /// </summary>
    Task<IEnumerable<VoiceNote>> GetAllForUserAsync(string userId, int? limit = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated voice notes for a user (efficient database-level pagination)
    /// </summary>
    Task<IEnumerable<VoiceNote>> GetPagedForUserAsync(string userId, int skip, int take, CancellationToken cancellationToken = default);
}

