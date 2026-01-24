namespace ProjectBrain.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectBrain.Domain.Caching;
using ProjectBrain.Domain.UnitOfWork;

public class ApplicationSettingsService : IApplicationSettingsService
{
    private readonly AppDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly ILogger<ApplicationSettingsService> _logger;
    private const string SettingsCacheKeyPrefix = "appsettings:";
    private static readonly TimeSpan SettingsCacheExpiration = TimeSpan.FromMinutes(30);

    public ApplicationSettingsService(
        AppDbContext context,
        IUnitOfWork unitOfWork,
        ICacheService cache,
        ILogger<ApplicationSettingsService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string?> GetSettingAsync(string key)
    {
        // Try cache first
        var cacheKey = $"{SettingsCacheKeyPrefix}{key}";
        var cachedSetting = await _cache.GetAsync<ApplicationSetting>(cacheKey);
        if (cachedSetting != null)
        {
            return cachedSetting.Value;
        }

        var setting = await _context.ApplicationSettings
            .FirstOrDefaultAsync(s => s.Key == key);

        if (setting == null)
        {
            return null;
        }

        // Cache the setting
        await _cache.SetAsync(cacheKey, setting, SettingsCacheExpiration);
        return setting.Value;
    }

    public async Task<List<ApplicationSetting>> GetAllSettingsAsync()
    {
        return await _context.ApplicationSettings
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Key)
            .ToListAsync();
    }

    public async Task<List<ApplicationSetting>> GetSettingsByCategoryAsync(string category)
    {
        return await _context.ApplicationSettings
            .Where(s => s.Category == category)
            .OrderBy(s => s.Key)
            .ToListAsync();
    }

    public async Task UpdateSettingAsync(string key, string value, string updatedBy)
    {
        var setting = await _context.ApplicationSettings
            .FirstOrDefaultAsync(s => s.Key == key);

        if (setting == null)
        {
            throw new InvalidOperationException($"Setting with key '{key}' not found");
        }

        setting.Value = value;
        setting.UpdatedBy = updatedBy;
        setting.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        // Invalidate cache
        var cacheKey = $"{SettingsCacheKeyPrefix}{key}";
        await _cache.RemoveAsync(cacheKey);

        _logger.LogInformation("Application setting '{Key}' updated by {UpdatedBy}", key, updatedBy);
    }

    public async Task<AISettings> GetAISettingsAsync()
    {
        var maxSearchResults = await GetSettingAsync("AI:MaxSearchResults");
        var maxContentLengthPerSource = await GetSettingAsync("AI:MaxContentLengthPerSource");
        var maxHistoryMessages = await GetSettingAsync("AI:MaxHistoryMessages");
        var maxTotalTokens = await GetSettingAsync("AI:MaxTotalTokens");

        return new AISettings
        {
            MaxSearchResults = int.TryParse(maxSearchResults, out var searchResults) ? searchResults : 5,
            MaxContentLengthPerSource = int.TryParse(maxContentLengthPerSource, out var contentLength) ? contentLength : 800,
            MaxHistoryMessages = int.TryParse(maxHistoryMessages, out var historyMessages) ? historyMessages : 10,
            MaxTotalTokens = int.TryParse(maxTotalTokens, out var totalTokens) ? totalTokens : 7000
        };
    }

    public async Task UpdateAISettingsAsync(AISettings settings, string updatedBy)
    {
        await UpdateSettingAsync("AI:MaxSearchResults", settings.MaxSearchResults.ToString(), updatedBy);
        await UpdateSettingAsync("AI:MaxContentLengthPerSource", settings.MaxContentLengthPerSource.ToString(), updatedBy);
        await UpdateSettingAsync("AI:MaxHistoryMessages", settings.MaxHistoryMessages.ToString(), updatedBy);
        await UpdateSettingAsync("AI:MaxTotalTokens", settings.MaxTotalTokens.ToString(), updatedBy);
    }
}

public interface IApplicationSettingsService
{
    Task<string?> GetSettingAsync(string key);
    Task<List<ApplicationSetting>> GetAllSettingsAsync();
    Task<List<ApplicationSetting>> GetSettingsByCategoryAsync(string category);
    Task UpdateSettingAsync(string key, string value, string updatedBy);
    Task<AISettings> GetAISettingsAsync();
    Task UpdateAISettingsAsync(AISettings settings, string updatedBy);
}

public class AISettings
{
    public int MaxSearchResults { get; set; }
    public int MaxContentLengthPerSource { get; set; }
    public int MaxHistoryMessages { get; set; }
    public int MaxTotalTokens { get; set; }
}
