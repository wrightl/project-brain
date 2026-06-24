using ProjectBrain.Api.Background;
using ProjectBrain.Domain;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces.Managers;

namespace ProjectBrain.Api.Services;

public sealed class CopingStrategySideEffectsService : ICopingStrategySideEffects
{
    private readonly ITimeTickerManager<TimeTickerEntity> _timeTickerManager;
    private readonly IMemoryPromotionService _memoryPromotionService;
    private readonly ILogger<CopingStrategySideEffectsService> _logger;

    public CopingStrategySideEffectsService(
        ITimeTickerManager<TimeTickerEntity> timeTickerManager,
        IMemoryPromotionService memoryPromotionService,
        ILogger<CopingStrategySideEffectsService> logger)
    {
        _timeTickerManager = timeTickerManager;
        _memoryPromotionService = memoryPromotionService;
        _logger = logger;
    }

    public async Task OnStrategyCreatedAsync(
        string userId,
        Guid strategyId,
        string title,
        string description,
        string? iconKey,
        int? rating,
        DateTime savedAt,
        Guid? conversationId,
        CancellationToken cancellationToken = default)
    {
        await UserContextTickerEnqueue.EnqueueStrategyUploadAsync(_timeTickerManager, new StrategyUploadRequest
        {
            UserId = userId,
            StrategyId = strategyId,
            Title = title,
            Description = description,
            IconKey = iconKey,
            Rating = rating,
            SavedAt = savedAt
        });

        try
        {
            await _memoryPromotionService.PromoteStrategyEpisodeAsync(
                userId,
                strategyId,
                title,
                conversationId,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create episodic memory for strategy {StrategyId}", strategyId);
        }
    }

    public Task EnqueueStrategyReindexAsync(
        string userId,
        Guid strategyId,
        string title,
        string description,
        string? iconKey,
        int? rating,
        DateTime savedAt,
        CancellationToken cancellationToken = default)
    {
        return UserContextTickerEnqueue.EnqueueStrategyUploadAsync(_timeTickerManager, new StrategyUploadRequest
        {
            UserId = userId,
            StrategyId = strategyId,
            Title = title,
            Description = description,
            IconKey = iconKey,
            Rating = rating,
            SavedAt = savedAt
        });
    }
}
