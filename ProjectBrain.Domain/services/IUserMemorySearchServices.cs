namespace ProjectBrain.Domain;

using ProjectBrain.Domain.Dtos;

public interface IUserMemoryRetrievalService
{
    Task<MemoryRetrievalResult> SearchAsync(
        string userId,
        string query,
        MemorySettings settings,
        CancellationToken cancellationToken = default);
}

public interface IUserMemoryIndexService
{
    Task IndexFactAsync(UserFact fact, CancellationToken cancellationToken = default);
    Task IndexEpisodeAsync(UserEpisode episode, CancellationToken cancellationToken = default);
    Task DeleteFactAsync(Guid factId, CancellationToken cancellationToken = default);
    Task DeleteEpisodeAsync(Guid episodeId, CancellationToken cancellationToken = default);
    Task DeleteAllForUserAsync(string userId, CancellationToken cancellationToken = default);
}
