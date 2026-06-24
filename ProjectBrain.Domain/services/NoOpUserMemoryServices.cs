namespace ProjectBrain.Domain;

using ProjectBrain.Domain.Dtos;
using ProjectBrain.Domain.Repositories;

/// <summary>No-op fallback when search indexing is not configured (e.g. design-time).</summary>
public sealed class NoOpUserMemoryIndexService : IUserMemoryIndexService
{
    public Task IndexFactAsync(UserFact fact, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task IndexEpisodeAsync(UserEpisode episode, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task DeleteFactAsync(Guid factId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task DeleteEpisodeAsync(Guid episodeId, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

/// <summary>SQL-only retrieval fallback when hybrid search is unavailable.</summary>
public sealed class SqlUserMemoryRetrievalService : IUserMemoryRetrievalService
{
    private readonly IUserFactRepository _factRepository;
    private readonly IUserEpisodeRepository _episodeRepository;

    public SqlUserMemoryRetrievalService(
        IUserFactRepository factRepository,
        IUserEpisodeRepository episodeRepository)
    {
        _factRepository = factRepository;
        _episodeRepository = episodeRepository;
    }

    public async Task<MemoryRetrievalResult> SearchAsync(
        string userId,
        string query,
        MemorySettings settings,
        CancellationToken cancellationToken = default)
    {
        if (!settings.EnableMemoryFormation || string.IsNullOrWhiteSpace(query))
        {
            return new MemoryRetrievalResult { RetrievalMode = "disabled" };
        }

        var facts = await _factRepository.SearchActiveByContentAsync(
            userId, query, settings.MaxFactsRetrieved, cancellationToken);
        var episodes = await _episodeRepository.SearchActiveByContentAsync(
            userId, query, settings.MaxEpisodesRetrieved, cancellationToken);

        return new MemoryRetrievalResult
        {
            RetrievalMode = "sql_fallback",
            Facts = facts.Select(f => new RetrievedUserFact
            {
                Id = f.Id,
                Content = f.Content,
                Category = f.Category
            }).ToList(),
            Episodes = episodes.Select(e => new RetrievedUserEpisode
            {
                Id = e.Id,
                Summary = e.Summary,
                Topic = e.Topic,
                Outcome = e.Outcome
            }).ToList()
        };
    }
}
