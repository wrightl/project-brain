namespace ProjectBrain.Domain;

using Microsoft.Extensions.Logging;
using ProjectBrain.Domain.Caching;
using ProjectBrain.Domain.Dtos;
using ProjectBrain.Domain.Repositories;

public class UserErasureService : IUserErasureService
{
    private const string ProfileCacheKeyPrefix = "userprofile:";

    private readonly ISubscriptionService _subscriptionService;
    private readonly IUserSearchIndexErasureService _searchIndexErasure;
    private readonly IUserBlobErasureService _blobErasure;
    private readonly IMemoryPromotionAuditRepository _memoryPromotionAuditRepository;
    private readonly IQuizResponseRepository _quizResponseRepository;
    private readonly ITagRepository _tagRepository;
    private readonly ICoachMessageService _coachMessageService;
    private readonly IUserFactRepository _userFactRepository;
    private readonly IUserEpisodeRepository _userEpisodeRepository;
    private readonly IUserMemoryIndexService _userMemoryIndexService;
    private readonly IUserService _userService;
    private readonly ICacheService _cache;
    private readonly ILogger<UserErasureService> _logger;

    private static readonly string[] AllMemoryStatuses =
    [
        MemoryStatuses.Provisional,
        MemoryStatuses.Active,
        MemoryStatuses.Superseded,
        MemoryStatuses.Rejected
    ];

    public UserErasureService(
        ISubscriptionService subscriptionService,
        IUserSearchIndexErasureService searchIndexErasure,
        IUserBlobErasureService blobErasure,
        IMemoryPromotionAuditRepository memoryPromotionAuditRepository,
        IQuizResponseRepository quizResponseRepository,
        ITagRepository tagRepository,
        ICoachMessageService coachMessageService,
        IUserFactRepository userFactRepository,
        IUserEpisodeRepository userEpisodeRepository,
        IUserMemoryIndexService userMemoryIndexService,
        IUserService userService,
        ICacheService cache,
        ILogger<UserErasureService> logger)
    {
        _subscriptionService = subscriptionService;
        _searchIndexErasure = searchIndexErasure;
        _blobErasure = blobErasure;
        _memoryPromotionAuditRepository = memoryPromotionAuditRepository;
        _quizResponseRepository = quizResponseRepository;
        _tagRepository = tagRepository;
        _coachMessageService = coachMessageService;
        _userFactRepository = userFactRepository;
        _userEpisodeRepository = userEpisodeRepository;
        _userMemoryIndexService = userMemoryIndexService;
        _userService = userService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ErasureResult> EraseUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var result = new ErasureResult { UserId = userId };

        await TryCancelSubscriptionAsync(userId, UserType.User, result, cancellationToken);
        await TryCancelSubscriptionAsync(userId, UserType.Coach, result, cancellationToken);

        var externalErasureFailed = false;
        try
        {
            result.SearchDocumentsDeleted = await _searchIndexErasure.DeleteAllDocumentsForUserAsync(userId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search index erasure failed for user {UserId}", userId);
            result.Warnings.Add($"Search index erasure failed: {ex.Message}");
            externalErasureFailed = true;
        }

        try
        {
            result.BlobFilesDeleted = await _blobErasure.DeleteAllUserFilesAsync(userId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blob erasure failed for user {UserId}", userId);
            result.Warnings.Add($"Blob erasure failed: {ex.Message}");
            externalErasureFailed = true;
        }

        if (externalErasureFailed)
        {
            throw new InvalidOperationException(
                $"External user erasure failed for {userId}; retaining user row so erasure can be retried.");
        }

        result.MemoryPromotionAuditsDeleted =
            await _memoryPromotionAuditRepository.DeleteByUserIdAsync(userId, cancellationToken);
        result.QuizResponsesDeleted =
            await _quizResponseRepository.DeleteByUserIdAsync(userId, cancellationToken);
        result.TagsDeleted =
            await _tagRepository.DeleteByUserIdAsync(userId, cancellationToken);
        result.CoachMessagesDeleted =
            await _coachMessageService.DeleteAllForUserAsync(userId, cancellationToken);

        result.MemoryIndexEntriesDeleted = await DeleteUserMemoryIndexEntriesAsync(userId, cancellationToken);

        await _userService.DeleteById(userId);
        result.UserRowDeleted = true;

        await _cache.RemoveAsync($"{ProfileCacheKeyPrefix}{userId}");

        _logger.LogInformation(
            "Completed user erasure for {UserId}: search={Search}, blobs={Blobs}, audits={Audits}",
            userId,
            result.SearchDocumentsDeleted,
            result.BlobFilesDeleted,
            result.MemoryPromotionAuditsDeleted);

        return result;
    }

    private async Task TryCancelSubscriptionAsync(
        string userId,
        UserType userType,
        ErasureResult result,
        CancellationToken cancellationToken)
    {
        try
        {
            var subscription = await _subscriptionService.GetUserSubscriptionAsync(userId, userType);
            if (subscription is null)
            {
                return;
            }

            await _subscriptionService.CancelSubscriptionAsync(userId, userType);
            result.SubscriptionCanceled = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cancel {UserType} subscription for user {UserId}", userType, userId);
            result.Warnings.Add($"Subscription cancel ({userType}) failed: {ex.Message}");
        }
    }

    private async Task<int> DeleteUserMemoryIndexEntriesAsync(string userId, CancellationToken cancellationToken)
    {
        var deleted = 0;
        var facts = await _userFactRepository.GetForUserByStatusesAsync(userId, AllMemoryStatuses, cancellationToken);
        foreach (var fact in facts)
        {
            await _userMemoryIndexService.DeleteFactAsync(fact.Id, cancellationToken);
            deleted++;
        }

        var episodes = await _userEpisodeRepository.GetForUserByStatusesAsync(userId, AllMemoryStatuses, cancellationToken);
        foreach (var episode in episodes)
        {
            await _userMemoryIndexService.DeleteEpisodeAsync(episode.Id, cancellationToken);
            deleted++;
        }

        await _userMemoryIndexService.DeleteAllForUserAsync(userId, cancellationToken);
        return deleted;
    }
}

public interface IUserErasureService
{
    Task<ErasureResult> EraseUserAsync(string userId, CancellationToken cancellationToken = default);
}
