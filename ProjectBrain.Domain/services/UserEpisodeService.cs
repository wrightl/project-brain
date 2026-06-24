namespace ProjectBrain.Domain;

using ProjectBrain.Domain.Dtos;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

public class UserEpisodeService : IUserEpisodeService
{
    private readonly IUserEpisodeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UserEpisodeService(IUserEpisodeRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserEpisode?> GetByIdForUserAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByIdForUserAsync(id, userId, cancellationToken);
    }

    public async Task<UserEpisode?> GetByContentHashAsync(string userId, string contentHash, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByContentHashAsync(userId, contentHash, cancellationToken);
    }

    public async Task<UserEpisode> AddAsync(UserEpisode episode, CancellationToken cancellationToken = default)
    {
        _repository.Add(episode);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return episode;
    }

    public async Task UpdateAsync(UserEpisode episode, CancellationToken cancellationToken = default)
    {
        episode.UpdatedAt = DateTime.UtcNow;
        _repository.Update(episode);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> SupersedeAsync(string userId, Guid id, CancellationToken cancellationToken = default)
    {
        var tracked = await _repository.GetByIdAsync(id, cancellationToken);
        if (tracked is null || tracked.UserId != userId)
        {
            return false;
        }

        tracked.Status = MemoryStatuses.Superseded;
        tracked.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<UserEpisodeDto>> ListForUserAsync(
        string userId,
        bool includeProvisional,
        CancellationToken cancellationToken = default)
    {
        var statuses = includeProvisional
            ? new[] { MemoryStatuses.Active, MemoryStatuses.Provisional }
            : new[] { MemoryStatuses.Active };

        var episodes = await _repository.GetForUserByStatusesAsync(userId, statuses, cancellationToken);
        return episodes.Select(e => new UserEpisodeDto
        {
            Id = e.Id,
            Summary = e.Summary,
            Topic = e.Topic,
            Outcome = e.Outcome,
            Status = e.Status,
            CreatedAt = e.CreatedAt,
            IsPinned = e.PinnedAt is not null
        }).ToList();
    }

    public Task TouchRetrievedAsync(IReadOnlyList<Guid> episodeIds, CancellationToken cancellationToken = default)
    {
        return _repository.TouchRetrievedAsync(episodeIds, cancellationToken);
    }

    public async Task<bool> PinAsync(string userId, Guid id, CancellationToken cancellationToken = default)
    {
        var episode = await _repository.GetByIdAsync(id, cancellationToken);
        if (episode is null || episode.UserId != userId || episode.Status is MemoryStatuses.Superseded or MemoryStatuses.Rejected)
        {
            return false;
        }

        episode.PinnedAt = DateTime.UtcNow;
        episode.ExpiresAt = null;
        if (episode.Status == MemoryStatuses.Provisional)
        {
            episode.Status = MemoryStatuses.Active;
        }

        episode.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UnpinAsync(string userId, Guid id, int activeTtlDays, CancellationToken cancellationToken = default)
    {
        var episode = await _repository.GetByIdAsync(id, cancellationToken);
        if (episode is null || episode.UserId != userId || episode.PinnedAt is null)
        {
            return false;
        }

        episode.PinnedAt = null;
        episode.ExpiresAt = DateTime.UtcNow.AddDays(activeTtlDays);
        episode.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public interface IUserEpisodeService
{
    Task<UserEpisode?> GetByIdForUserAsync(Guid id, string userId, CancellationToken cancellationToken = default);
    Task<UserEpisode?> GetByContentHashAsync(string userId, string contentHash, CancellationToken cancellationToken = default);
    Task<UserEpisode> AddAsync(UserEpisode episode, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserEpisode episode, CancellationToken cancellationToken = default);
    Task<bool> SupersedeAsync(string userId, Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserEpisodeDto>> ListForUserAsync(string userId, bool includeProvisional, CancellationToken cancellationToken = default);
    Task TouchRetrievedAsync(IReadOnlyList<Guid> episodeIds, CancellationToken cancellationToken = default);
    Task<bool> PinAsync(string userId, Guid id, CancellationToken cancellationToken = default);
    Task<bool> UnpinAsync(string userId, Guid id, int activeTtlDays, CancellationToken cancellationToken = default);
}
