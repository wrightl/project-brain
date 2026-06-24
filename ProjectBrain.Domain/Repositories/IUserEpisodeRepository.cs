namespace ProjectBrain.Domain.Repositories;

public interface IUserEpisodeRepository : IRepository<UserEpisode, Guid>
{
    Task<UserEpisode?> GetByIdForUserAsync(Guid id, string userId, CancellationToken cancellationToken = default);
    Task<UserEpisode?> GetByContentHashAsync(string userId, string contentHash, CancellationToken cancellationToken = default);
    Task<List<UserEpisode>> GetForUserByStatusesAsync(string userId, IReadOnlyList<string> statuses, CancellationToken cancellationToken = default);
    Task<List<UserEpisode>> SearchActiveByContentAsync(string userId, string query, int limit, CancellationToken cancellationToken = default);
    Task<List<UserEpisode>> GetDecayCandidatesAsync(string? userId, CancellationToken cancellationToken = default);
    Task TouchRetrievedAsync(IReadOnlyList<Guid> episodeIds, CancellationToken cancellationToken = default);
}
