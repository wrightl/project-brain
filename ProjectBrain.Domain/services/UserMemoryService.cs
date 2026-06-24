namespace ProjectBrain.Domain;

using ProjectBrain.Domain.Dtos;

public class UserMemoryService : IUserMemoryService
{
    private readonly IUserFactService _factService;
    private readonly IUserEpisodeService _episodeService;
    private readonly IUserMemoryIndexService _memoryIndexService;

    public UserMemoryService(
        IUserFactService factService,
        IUserEpisodeService episodeService,
        IUserMemoryIndexService memoryIndexService)
    {
        _factService = factService;
        _episodeService = episodeService;
        _memoryIndexService = memoryIndexService;
    }

    public async Task<UserMemoryListDto> ListAsync(string userId, CancellationToken cancellationToken = default)
    {
        var factList = await _factService.ListForUserAsync(userId, includeProvisional: false, cancellationToken);
        var episodes = await _episodeService.ListForUserAsync(userId, includeProvisional: false, cancellationToken);
        return new UserMemoryListDto
        {
            Facts = factList.Facts,
            Episodes = episodes
        };
    }

    public async Task<bool> DeleteFactAsync(string userId, Guid id, CancellationToken cancellationToken = default)
    {
        var superseded = await _factService.SupersedeAsync(userId, id, cancellationToken);
        if (!superseded)
        {
            return false;
        }

        await _memoryIndexService.DeleteFactAsync(id, cancellationToken);
        return true;
    }

    public async Task<bool> DeleteEpisodeAsync(string userId, Guid id, CancellationToken cancellationToken = default)
    {
        var superseded = await _episodeService.SupersedeAsync(userId, id, cancellationToken);
        if (!superseded)
        {
            return false;
        }

        await _memoryIndexService.DeleteEpisodeAsync(id, cancellationToken);
        return true;
    }

    public async Task RecordRetrievalAsync(
        IReadOnlyList<Guid> factIds,
        IReadOnlyList<Guid> episodeIds,
        CancellationToken cancellationToken = default)
    {
        await _factService.TouchRetrievedAsync(factIds, cancellationToken);
        await _episodeService.TouchRetrievedAsync(episodeIds, cancellationToken);
    }
}

public interface IUserMemoryService
{
    Task<UserMemoryListDto> ListAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteFactAsync(string userId, Guid id, CancellationToken cancellationToken = default);
    Task<bool> DeleteEpisodeAsync(string userId, Guid id, CancellationToken cancellationToken = default);
    Task RecordRetrievalAsync(
        IReadOnlyList<Guid> factIds,
        IReadOnlyList<Guid> episodeIds,
        CancellationToken cancellationToken = default);
}
