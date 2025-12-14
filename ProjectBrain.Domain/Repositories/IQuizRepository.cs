namespace ProjectBrain.Domain.Repositories;

/// <summary>
/// Repository interface for Quiz entity with domain-specific queries
/// </summary>
public interface IQuizRepository : IRepository<Quiz, Guid>
{
    /// <summary>
    /// Gets a quiz by ID with questions ordered by QuestionOrder
    /// </summary>
    Task<Quiz?> GetByIdWithQuestionsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all quizzes ordered by creation date
    /// </summary>
    Task<IEnumerable<Quiz>> GetAllOrderedByDateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated quizzes ordered by creation date (efficient database-level pagination)
    /// </summary>
    Task<IEnumerable<Quiz>> GetPagedOrderedByDateAsync(int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts all quizzes
    /// </summary>
    Task<int> CountAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a quiz has any responses
    /// </summary>
    Task<bool> HasResponsesAsync(Guid quizId, CancellationToken cancellationToken = default);
}

