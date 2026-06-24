namespace ProjectBrain.Domain;

using Microsoft.Extensions.Logging;
using ProjectBrain.Domain.Dtos;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

public class MemoryPromotionService : IMemoryPromotionService
{
    private readonly IUserFactRepository _factRepository;
    private readonly IUserEpisodeRepository _episodeRepository;
    private readonly IMemoryPromotionAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserMemoryIndexService _memoryIndexService;
    private readonly ILogger<MemoryPromotionService> _logger;

    public MemoryPromotionService(
        IUserFactRepository factRepository,
        IUserEpisodeRepository episodeRepository,
        IMemoryPromotionAuditRepository auditRepository,
        IUnitOfWork unitOfWork,
        IUserMemoryIndexService memoryIndexService,
        ILogger<MemoryPromotionService> logger)
    {
        _factRepository = factRepository;
        _episodeRepository = episodeRepository;
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
        _memoryIndexService = memoryIndexService;
        _logger = logger;
    }

    public async Task ProcessExtractionAsync(
        string userId,
        Guid? conversationId,
        MemoryExtractionResult extraction,
        MemorySettings settings,
        CancellationToken cancellationToken = default)
    {
        if (!settings.EnableMemoryFormation)
        {
            return;
        }

        var facts = extraction.Facts.Take(settings.MaxFactsPerTurn).ToList();
        var episodes = extraction.Episodes.Take(settings.MaxEpisodesPerTurn).ToList();

        foreach (var candidate in facts)
        {
            await ProcessFactCandidateAsync(userId, conversationId, candidate, settings, cancellationToken);
        }

        foreach (var candidate in episodes)
        {
            await ProcessEpisodeCandidateAsync(userId, conversationId, candidate, settings, cancellationToken);
        }
    }

    public async Task<UserEpisode> PromoteStrategyEpisodeAsync(
        string userId,
        Guid strategyId,
        string strategyTitle,
        Guid? conversationId,
        CancellationToken cancellationToken = default)
    {
        var summary = $"User saved coping strategy '{strategyTitle}' after chat suggestions.";
        var contentHash = MemoryHashHelper.ComputeHash(summary);
        var existing = await _episodeRepository.GetByContentHashAsync(userId, contentHash, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var episode = new UserEpisode
        {
            UserId = userId,
            Summary = summary,
            Topic = "coping_strategies",
            Outcome = "helpful",
            RelatedStrategyId = strategyId,
            Status = MemoryStatuses.Active,
            Confidence = 1.0,
            ContentHash = contentHash,
            SourceConversationId = conversationId,
            ObservationCount = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _episodeRepository.Add(episode);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _memoryIndexService.IndexEpisodeAsync(episode, cancellationToken);

        _logger.LogInformation(
            "[MemoryPromotion] Fast-path episode created for strategy {StrategyId} user {UserId}",
            strategyId,
            userId);

        return episode;
    }

    public async Task BootstrapFactsFromOnboardingAsync(
        string userId,
        object onboardingData,
        MemorySettings settings,
        CancellationToken cancellationToken = default)
    {
        if (!settings.EnableMemoryFormation || onboardingData is not Dictionary<string, object?> data)
        {
            return;
        }

        var candidates = new List<ExtractedFactCandidate>();

        if (data.TryGetValue("preferredPronoun", out var pronoun) && !string.IsNullOrWhiteSpace(pronoun?.ToString()))
        {
            candidates.Add(new ExtractedFactCandidate
            {
                Content = $"Preferred pronoun: {pronoun}",
                Category = "preference",
                Confidence = 0.95
            });
        }

        if (data.TryGetValue("neurodiverseTraits", out var traits) && traits is IEnumerable<object> traitList)
        {
            var traitValues = traitList.Select(t => t?.ToString()).Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
            if (traitValues.Count > 0)
            {
                candidates.Add(new ExtractedFactCandidate
                {
                    Content = $"Neurodiverse traits: {string.Join(", ", traitValues)}",
                    Category = "profile",
                    Confidence = 0.95
                });
            }
        }

        if (data.TryGetValue("onboarding", out var onboardingObj) && onboardingObj is Dictionary<string, object?> onboarding)
        {
            foreach (var section in onboarding.Take(3))
            {
                if (section.Value is Dictionary<string, object?> sectionData && sectionData.Count > 0)
                {
                    var summary = string.Join("; ", sectionData
                        .Where(kv => kv.Value is not null && !string.IsNullOrWhiteSpace(kv.Value.ToString()))
                        .Take(3)
                        .Select(kv => $"{kv.Key}: {kv.Value}"));
                    if (!string.IsNullOrWhiteSpace(summary))
                    {
                        candidates.Add(new ExtractedFactCandidate
                        {
                            Content = $"{section.Key}: {summary}",
                            Category = "onboarding",
                            Confidence = 0.9
                        });
                    }
                }
            }
        }

        foreach (var candidate in candidates.Take(5))
        {
            await ProcessFactCandidateAsync(userId, null, candidate, settings, cancellationToken);
        }
    }

    private async Task ProcessFactCandidateAsync(
        string userId,
        Guid? conversationId,
        ExtractedFactCandidate candidate,
        MemorySettings settings,
        CancellationToken cancellationToken)
    {
        var content = candidate.Content.Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        var hash = MemoryHashHelper.ComputeHash(content);

        if (candidate.Confidence < settings.ProvisionalConfidence)
        {
            await AuditAsync(userId, conversationId, "fact", content, "rejected", "below_provisional_confidence", cancellationToken);
            return;
        }

        var existing = await _factRepository.GetByContentHashAsync(userId, hash, cancellationToken);
        if (existing is not null)
        {
            existing.ObservationCount++;
            existing.UpdatedAt = DateTime.UtcNow;

            if (existing.Status == MemoryStatuses.Provisional
                && existing.ObservationCount >= settings.ActivationObservationCount)
            {
                existing.Status = MemoryStatuses.Active;
                await AuditAsync(userId, conversationId, "fact", content, "promoted", "repeat_observation", cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _memoryIndexService.IndexFactAsync(existing, cancellationToken);
            }
            else
            {
                await AuditAsync(userId, conversationId, "fact", content, "provisional", "duplicate", cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        if (candidate.Confidence < settings.MinPromotionConfidence)
        {
            if (candidate.Confidence >= settings.ProvisionalConfidence)
            {
                var provisional = new UserFact
                {
                    UserId = userId,
                    Content = content,
                    Category = NormalizeCategory(candidate.Category),
                    Status = MemoryStatuses.Provisional,
                    Confidence = candidate.Confidence,
                    ContentHash = hash,
                    SourceConversationId = conversationId,
                    ObservationCount = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _factRepository.Add(provisional);
                await AuditAsync(userId, conversationId, "fact", content, "provisional", "below_promotion_threshold", cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (settings.IndexProvisionalMemories)
                {
                    await _memoryIndexService.IndexFactAsync(provisional, cancellationToken);
                }
            }
            else
            {
                await AuditAsync(userId, conversationId, "fact", content, "rejected", "below_provisional_confidence", cancellationToken);
            }

            return;
        }

        await SupersedeContradictingFactsAsync(userId, NormalizeCategory(candidate.Category), cancellationToken);

        var fact = new UserFact
        {
            UserId = userId,
            Content = content,
            Category = NormalizeCategory(candidate.Category),
            Status = MemoryStatuses.Active,
            Confidence = candidate.Confidence,
            ContentHash = hash,
            SourceConversationId = conversationId,
            ObservationCount = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _factRepository.Add(fact);
        await AuditAsync(userId, conversationId, "fact", content, "promoted", "confidence_met", cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _memoryIndexService.IndexFactAsync(fact, cancellationToken);

        _logger.LogInformation("[MemoryPromotion] Promoted fact {FactId} for user {UserId}", fact.Id, userId);
    }

    private async Task ProcessEpisodeCandidateAsync(
        string userId,
        Guid? conversationId,
        ExtractedEpisodeCandidate candidate,
        MemorySettings settings,
        CancellationToken cancellationToken)
    {
        var summary = candidate.Summary.Trim();
        if (string.IsNullOrWhiteSpace(summary))
        {
            return;
        }

        var hash = MemoryHashHelper.ComputeHash(summary);

        if (candidate.Confidence < settings.ProvisionalConfidence)
        {
            await AuditAsync(userId, conversationId, "episode", summary, "rejected", "below_provisional_confidence", cancellationToken);
            return;
        }

        var existing = await _episodeRepository.GetByContentHashAsync(userId, hash, cancellationToken);
        if (existing is not null)
        {
            existing.ObservationCount++;
            existing.UpdatedAt = DateTime.UtcNow;

            if (existing.Status == MemoryStatuses.Provisional
                && existing.ObservationCount >= settings.ActivationObservationCount)
            {
                existing.Status = MemoryStatuses.Active;
                await AuditAsync(userId, conversationId, "episode", summary, "promoted", "repeat_observation", cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _memoryIndexService.IndexEpisodeAsync(existing, cancellationToken);
            }
            else
            {
                await AuditAsync(userId, conversationId, "episode", summary, "provisional", "duplicate", cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        if (candidate.Confidence < settings.MinPromotionConfidence)
        {
            if (candidate.Confidence >= settings.ProvisionalConfidence)
            {
                var provisional = new UserEpisode
                {
                    UserId = userId,
                    Summary = summary,
                    Topic = NormalizeTopic(candidate.Topic),
                    Outcome = NormalizeOutcome(candidate.Outcome),
                    Status = MemoryStatuses.Provisional,
                    Confidence = candidate.Confidence,
                    ContentHash = hash,
                    SourceConversationId = conversationId,
                    ObservationCount = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _episodeRepository.Add(provisional);
                await AuditAsync(userId, conversationId, "episode", summary, "provisional", "below_promotion_threshold", cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (settings.IndexProvisionalMemories)
                {
                    await _memoryIndexService.IndexEpisodeAsync(provisional, cancellationToken);
                }
            }
            else
            {
                await AuditAsync(userId, conversationId, "episode", summary, "rejected", "below_provisional_confidence", cancellationToken);
            }

            return;
        }

        var episode = new UserEpisode
        {
            UserId = userId,
            Summary = summary,
            Topic = NormalizeTopic(candidate.Topic),
            Outcome = NormalizeOutcome(candidate.Outcome),
            Status = MemoryStatuses.Active,
            Confidence = candidate.Confidence,
            ContentHash = hash,
            SourceConversationId = conversationId,
            ObservationCount = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _episodeRepository.Add(episode);
        await AuditAsync(userId, conversationId, "episode", summary, "promoted", "confidence_met", cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _memoryIndexService.IndexEpisodeAsync(episode, cancellationToken);

        _logger.LogInformation("[MemoryPromotion] Promoted episode {EpisodeId} for user {UserId}", episode.Id, userId);
    }

    private async Task SupersedeContradictingFactsAsync(
        string userId,
        string category,
        CancellationToken cancellationToken)
    {
        var activeInCategory = await _factRepository.FindAsync(
            f => f.UserId == userId && f.Category == category && f.Status == MemoryStatuses.Active,
            cancellationToken);

        foreach (var old in activeInCategory)
        {
            var tracked = await _factRepository.GetByIdAsync(old.Id, cancellationToken);
            if (tracked is null)
            {
                continue;
            }

            tracked.Status = MemoryStatuses.Superseded;
            tracked.UpdatedAt = DateTime.UtcNow;
            _factRepository.Update(tracked);
            await _memoryIndexService.DeleteFactAsync(tracked.Id, cancellationToken);
        }
    }

    private async Task AuditAsync(
        string userId,
        Guid? conversationId,
        string candidateType,
        string content,
        string decision,
        string reason,
        CancellationToken cancellationToken)
    {
        _auditRepository.Add(new MemoryPromotionAudit
        {
            UserId = userId,
            ConversationId = conversationId,
            CandidateType = candidateType,
            CandidateContent = content.Length > 1000 ? content[..1000] : content,
            Decision = decision,
            Reason = reason,
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "[MemoryPromotion] user={UserId} type={Type} decision={Decision} reason={Reason}",
            userId,
            candidateType,
            decision,
            reason);
    }

    private static string NormalizeCategory(string? category) =>
        string.IsNullOrWhiteSpace(category) ? "general" : category.Trim().ToLowerInvariant()[..Math.Min(category.Trim().Length, 50)];

    private static string NormalizeTopic(string? topic) =>
        string.IsNullOrWhiteSpace(topic) ? "general" : topic.Trim().ToLowerInvariant()[..Math.Min(topic.Trim().Length, 100)];

    private static string NormalizeOutcome(string? outcome)
    {
        var normalized = (outcome ?? "unknown").Trim().ToLowerInvariant();
        return normalized is "helpful" or "neutral" or "unhelpful" ? normalized : "unknown";
    }
}

public interface IMemoryPromotionService
{
    Task ProcessExtractionAsync(
        string userId,
        Guid? conversationId,
        MemoryExtractionResult extraction,
        MemorySettings settings,
        CancellationToken cancellationToken = default);

    Task<UserEpisode> PromoteStrategyEpisodeAsync(
        string userId,
        Guid strategyId,
        string strategyTitle,
        Guid? conversationId,
        CancellationToken cancellationToken = default);

    Task BootstrapFactsFromOnboardingAsync(
        string userId,
        object onboardingData,
        MemorySettings settings,
        CancellationToken cancellationToken = default);
}
