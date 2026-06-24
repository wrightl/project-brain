namespace ProjectBrain.Domain.Repositories;

public interface IUserFactRepository : IRepository<UserFact, Guid>
{
    Task<UserFact?> GetByIdForUserAsync(Guid id, string userId, CancellationToken cancellationToken = default);
    Task<UserFact?> GetByContentHashAsync(string userId, string contentHash, CancellationToken cancellationToken = default);
    Task<List<UserFact>> GetActiveForUserAsync(string userId, int limit, CancellationToken cancellationToken = default);
    Task<List<UserFact>> GetForUserByStatusesAsync(string userId, IReadOnlyList<string> statuses, CancellationToken cancellationToken = default);
    Task<List<UserFact>> SearchActiveByContentAsync(string userId, string query, int limit, CancellationToken cancellationToken = default);
    Task<List<UserFact>> GetDecayCandidatesAsync(string? userId, CancellationToken cancellationToken = default);
    Task TouchRetrievedAsync(IReadOnlyList<Guid> factIds, CancellationToken cancellationToken = default);
}
