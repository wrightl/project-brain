namespace ProjectBrain.Domain.Repositories;

/// <summary>
/// Repository interface for QuizResponse entity with domain-specific queries
/// </summary>
public interface IQuizResponseRepository : IRepository<QuizResponse, Guid>
{
    /// <summary>
    /// Gets a quiz response by ID for a specific user
    /// </summary>
    Task<QuizResponse?> GetByIdForUserAsync(Guid id, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a quiz response by quiz ID and user ID
    /// </summary>
    Task<QuizResponse?> GetByQuizAndUserAsync(Guid quizId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all quiz responses for a user
    /// </summary>
    Task<IEnumerable<QuizResponse>> GetAllForUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated quiz responses for a user (efficient database-level pagination)
    /// </summary>
    Task<IEnumerable<QuizResponse>> GetPagedForUserAsync(string userId, int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all quiz responses for a quiz and user
    /// </summary>
    Task<IEnumerable<QuizResponse>> GetByQuizForUserAsync(Guid quizId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all quiz responses for a quiz
    /// </summary>
    Task<IEnumerable<QuizResponse>> GetAllForQuizAsync(Guid quizId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts quiz responses for a user
    /// </summary>
    Task<int> CountForUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts quiz responses for a quiz
    /// </summary>
    Task<int> CountForQuizAsync(Guid quizId, CancellationToken cancellationToken = default);
}

