namespace ProjectBrain.Domain;

using ProjectBrain.Domain.Dtos;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

public class UserMemoryService : IUserMemoryService
{
    private readonly IUserFactService _factService;
    private readonly IUserEpisodeService _episodeService;
    private readonly IUserMemoryIndexService _memoryIndexService;
    private readonly IMemoryPromotionAuditRepository _auditRepository;
    private readonly IApplicationSettingsService _applicationSettingsService;
    private readonly IUnitOfWork _unitOfWork;

    public UserMemoryService(
        IUserFactService factService,
        IUserEpisodeService episodeService,
        IUserMemoryIndexService memoryIndexService,
        IMemoryPromotionAuditRepository auditRepository,
        IApplicationSettingsService applicationSettingsService,
        IUnitOfWork unitOfWork)
    {
        _factService = factService;
        _episodeService = episodeService;
        _memoryIndexService = memoryIndexService;
        _auditRepository = auditRepository;
        _applicationSettingsService = applicationSettingsService;
        _unitOfWork = unitOfWork;
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

    public async Task<bool> PinFactAsync(string userId, Guid id, CancellationToken cancellationToken = default)
    {
        var fact = await _factService.GetByIdForUserAsync(id, userId, cancellationToken);
        if (fact is null)
        {
            return false;
        }

        var pinned = await _factService.PinAsync(userId, id, cancellationToken);
        if (!pinned)
        {
            return false;
        }

        var updated = await _factService.GetByIdForUserAsync(id, userId, cancellationToken);
        if (updated is not null)
        {
            await _memoryIndexService.IndexFactAsync(updated, cancellationToken);
        }
        await WritePinAuditAsync(userId, "fact", fact.Content, cancellationToken);
        return true;
    }

    public async Task<bool> UnpinFactAsync(string userId, Guid id, CancellationToken cancellationToken = default)
    {
        var settings = await _applicationSettingsService.GetMemorySettingsAsync(cancellationToken);
        return await _factService.UnpinAsync(userId, id, settings.ActiveFactTtlDays, cancellationToken);
    }

    public async Task<bool> PinEpisodeAsync(string userId, Guid id, CancellationToken cancellationToken = default)
    {
        var episode = await _episodeService.GetByIdForUserAsync(id, userId, cancellationToken);
        if (episode is null)
        {
            return false;
        }

        var pinned = await _episodeService.PinAsync(userId, id, cancellationToken);
        if (!pinned)
        {
            return false;
        }

        var updated = await _episodeService.GetByIdForUserAsync(id, userId, cancellationToken);
        if (updated is not null)
        {
            await _memoryIndexService.IndexEpisodeAsync(updated, cancellationToken);
        }
        await WritePinAuditAsync(userId, "episode", episode.Summary, cancellationToken);
        return true;
    }

    public async Task<bool> UnpinEpisodeAsync(string userId, Guid id, CancellationToken cancellationToken = default)
    {
        var settings = await _applicationSettingsService.GetMemorySettingsAsync(cancellationToken);
        return await _episodeService.UnpinAsync(userId, id, settings.ActiveEpisodeTtlDays, cancellationToken);
    }

    public async Task RecordRetrievalAsync(
        IReadOnlyList<Guid> factIds,
        IReadOnlyList<Guid> episodeIds,
        CancellationToken cancellationToken = default)
    {
        await _factService.TouchRetrievedAsync(factIds, cancellationToken);
        await _episodeService.TouchRetrievedAsync(episodeIds, cancellationToken);
    }

    private async Task WritePinAuditAsync(
        string userId,
        string candidateType,
        string content,
        CancellationToken cancellationToken)
    {
        _auditRepository.Add(new MemoryPromotionAudit
        {
            UserId = userId,
            CandidateType = candidateType,
            CandidateContent = content.Length > 1000 ? content[..1000] : content,
            Decision = "accepted",
            Reason = "user_pinned",
            CreatedAt = DateTime.UtcNow
        });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public interface IUserMemoryService
{
    Task<UserMemoryListDto> ListAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteFactAsync(string userId, Guid id, CancellationToken cancellationToken = default);
    Task<bool> DeleteEpisodeAsync(string userId, Guid id, CancellationToken cancellationToken = default);
    Task<bool> PinFactAsync(string userId, Guid id, CancellationToken cancellationToken = default);
    Task<bool> UnpinFactAsync(string userId, Guid id, CancellationToken cancellationToken = default);
    Task<bool> PinEpisodeAsync(string userId, Guid id, CancellationToken cancellationToken = default);
    Task<bool> UnpinEpisodeAsync(string userId, Guid id, CancellationToken cancellationToken = default);
    Task RecordRetrievalAsync(
        IReadOnlyList<Guid> factIds,
        IReadOnlyList<Guid> episodeIds,
        CancellationToken cancellationToken = default);
}
