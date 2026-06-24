namespace ProjectBrain.Domain;

using Microsoft.Extensions.Logging;
using ProjectBrain.Domain.Dtos;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

public class MemoryDecayService : IMemoryDecayService
{
    private readonly IApplicationSettingsService _applicationSettingsService;
    private readonly IUserFactRepository _factRepository;
    private readonly IUserEpisodeRepository _episodeRepository;
    private readonly IUserMemoryIndexService _memoryIndexService;
    private readonly IMemoryPromotionAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MemoryDecayService> _logger;

    public MemoryDecayService(
        IApplicationSettingsService applicationSettingsService,
        IUserFactRepository factRepository,
        IUserEpisodeRepository episodeRepository,
        IUserMemoryIndexService memoryIndexService,
        IMemoryPromotionAuditRepository auditRepository,
        IUnitOfWork unitOfWork,
        ILogger<MemoryDecayService> logger)
    {
        _applicationSettingsService = applicationSettingsService;
        _factRepository = factRepository;
        _episodeRepository = episodeRepository;
        _memoryIndexService = memoryIndexService;
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<int> ApplyDecayAsync(string? userId = null, CancellationToken cancellationToken = default)
    {
        var settings = await _applicationSettingsService.GetMemorySettingsAsync(cancellationToken);
        if (!settings.EnableMemoryDecay)
        {
            return 0;
        }

        var now = DateTime.UtcNow;
        var expiredCount = 0;

        var facts = await _factRepository.GetDecayCandidatesAsync(userId, cancellationToken);
        foreach (var fact in facts)
        {
            if (!ShouldExpireFact(fact, settings, now))
            {
                continue;
            }

            await SupersedeFactAsync(fact, cancellationToken);
            expiredCount++;
        }

        var episodes = await _episodeRepository.GetDecayCandidatesAsync(userId, cancellationToken);
        foreach (var episode in episodes)
        {
            if (!ShouldExpireEpisode(episode, settings, now))
            {
                continue;
            }

            await SupersedeEpisodeAsync(episode, cancellationToken);
            expiredCount++;
        }

        if (expiredCount > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "[MemoryDecay] Superseded {Count} memories for user scope {UserId}",
                expiredCount,
                userId ?? "all");
        }

        return expiredCount;
    }

    private static bool ShouldExpireFact(UserFact fact, MemorySettings settings, DateTime now)
    {
        if (fact.Status == MemoryStatuses.Provisional)
        {
            return fact.CreatedAt <= now.AddDays(-settings.ProvisionalTtlDays);
        }

        if (fact.ExpiresAt is not null && fact.ExpiresAt <= now)
        {
            return true;
        }

        var lastActivity = fact.LastRetrievedAt ?? fact.UpdatedAt;
        var inactive = lastActivity <= now.AddDays(-settings.DecayInactivityDays);
        var tooOld = fact.CreatedAt <= now.AddDays(-settings.ActiveFactTtlDays);
        return inactive && tooOld;
    }

    private static bool ShouldExpireEpisode(UserEpisode episode, MemorySettings settings, DateTime now)
    {
        if (episode.Status == MemoryStatuses.Provisional)
        {
            return episode.CreatedAt <= now.AddDays(-settings.ProvisionalTtlDays);
        }

        if (episode.ExpiresAt is not null && episode.ExpiresAt <= now)
        {
            return true;
        }

        var lastActivity = episode.LastRetrievedAt ?? episode.UpdatedAt;
        var inactive = lastActivity <= now.AddDays(-settings.DecayInactivityDays);
        var tooOld = episode.CreatedAt <= now.AddDays(-settings.ActiveEpisodeTtlDays);
        return inactive && tooOld;
    }

    private async Task SupersedeFactAsync(UserFact fact, CancellationToken cancellationToken)
    {
        fact.Status = MemoryStatuses.Superseded;
        fact.UpdatedAt = DateTime.UtcNow;
        _factRepository.Update(fact);
        await _memoryIndexService.DeleteFactAsync(fact.Id, cancellationToken);
        _auditRepository.Add(CreateAudit(fact.UserId, "fact", fact.Content, fact.SourceConversationId));
    }

    private async Task SupersedeEpisodeAsync(UserEpisode episode, CancellationToken cancellationToken)
    {
        episode.Status = MemoryStatuses.Superseded;
        episode.UpdatedAt = DateTime.UtcNow;
        _episodeRepository.Update(episode);
        await _memoryIndexService.DeleteEpisodeAsync(episode.Id, cancellationToken);
        _auditRepository.Add(CreateAudit(episode.UserId, "episode", episode.Summary, episode.SourceConversationId));
    }

    private static MemoryPromotionAudit CreateAudit(
        string userId,
        string candidateType,
        string content,
        Guid? conversationId)
    {
        return new MemoryPromotionAudit
        {
            UserId = userId,
            ConversationId = conversationId,
            CandidateType = candidateType,
            CandidateContent = content.Length > 1000 ? content[..1000] : content,
            Decision = "rejected",
            Reason = "expired",
            CreatedAt = DateTime.UtcNow
        };
    }
}

public interface IMemoryDecayService
{
    Task<int> ApplyDecayAsync(string? userId = null, CancellationToken cancellationToken = default);
}
