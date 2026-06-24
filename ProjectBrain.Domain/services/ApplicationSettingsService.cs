namespace ProjectBrain.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectBrain.Domain.Caching;
using ProjectBrain.Domain.Dtos;
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

    public async Task UpsertSettingAsync(
        string key,
        string value,
        string category,
        string description,
        string updatedBy)
    {
        var setting = await _context.ApplicationSettings
            .FirstOrDefaultAsync(s => s.Key == key);

        if (setting == null)
        {
            _context.ApplicationSettings.Add(new ApplicationSetting
            {
                Key = key,
                Value = value,
                Category = category,
                Description = description,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = updatedBy
            });

            await _unitOfWork.SaveChangesAsync();

            // Invalidate cache (if anything tried to read it earlier as missing)
            var cacheKey = $"{SettingsCacheKeyPrefix}{key}";
            await _cache.RemoveAsync(cacheKey);

            _logger.LogInformation("Application setting '{Key}' created by {UpdatedBy}", key, updatedBy);
            return;
        }

        setting.Value = value;
        setting.Category ??= category;
        setting.Description ??= description;
        setting.UpdatedBy = updatedBy;
        setting.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        // Invalidate cache
        var existingCacheKey = $"{SettingsCacheKeyPrefix}{key}";
        await _cache.RemoveAsync(existingCacheKey);

        _logger.LogInformation("Application setting '{Key}' upserted by {UpdatedBy}", key, updatedBy);
    }

    public async Task<AISettings> GetAISettingsAsync()
    {
        var maxSearchResults = await GetSettingAsync("AI:MaxSearchResults");
        var maxContentLengthPerSource = await GetSettingAsync("AI:MaxContentLengthPerSource");
        var maxHistoryMessages = await GetSettingAsync("AI:MaxHistoryMessages");
        var maxTotalTokens = await GetSettingAsync("AI:MaxTotalTokens");
        var recentMessageWindow = await GetSettingAsync("AI:RecentMessageWindow");
        var conversationSummaryInterval = await GetSettingAsync("AI:ConversationSummaryInterval");
        var maxConversationSummaryLength = await GetSettingAsync("AI:MaxConversationSummaryLength");
        var enableConversationSummary = await GetSettingAsync("AI:EnableConversationSummary");

        return new AISettings
        {
            MaxSearchResults = int.TryParse(maxSearchResults, out var searchResults) ? searchResults : 5,
            MaxContentLengthPerSource = int.TryParse(maxContentLengthPerSource, out var contentLength) ? contentLength : 800,
            MaxHistoryMessages = int.TryParse(maxHistoryMessages, out var historyMessages) ? historyMessages : 10,
            MaxTotalTokens = int.TryParse(maxTotalTokens, out var totalTokens) ? totalTokens : 7000,
            RecentMessageWindow = int.TryParse(recentMessageWindow, out var recentWindow) ? recentWindow : 4,
            ConversationSummaryInterval = int.TryParse(conversationSummaryInterval, out var summaryInterval) ? summaryInterval : 6,
            MaxConversationSummaryLength = int.TryParse(maxConversationSummaryLength, out var maxSummaryLen) ? maxSummaryLen : 1500,
            EnableConversationSummary = !bool.TryParse(enableConversationSummary, out var enableSummary) || enableSummary
        };
    }

    public async Task<ChatMemorySettings> GetChatMemorySettingsAsync()
    {
        var recentMessageWindow = await GetSettingAsync("AI:RecentMessageWindow");
        var conversationSummaryInterval = await GetSettingAsync("AI:ConversationSummaryInterval");
        var maxConversationSummaryLength = await GetSettingAsync("AI:MaxConversationSummaryLength");
        var enableConversationSummary = await GetSettingAsync("AI:EnableConversationSummary");

        return new ChatMemorySettings
        {
            RecentMessageWindow = int.TryParse(recentMessageWindow, out var recentWindow) ? recentWindow : 4,
            ConversationSummaryInterval = int.TryParse(conversationSummaryInterval, out var summaryInterval) ? summaryInterval : 6,
            MaxConversationSummaryLength = int.TryParse(maxConversationSummaryLength, out var maxSummaryLen) ? maxSummaryLen : 1500,
            EnableConversationSummary = !bool.TryParse(enableConversationSummary, out var enableSummary) || enableSummary
        };
    }

    public async Task UpdateChatMemorySettingsAsync(ChatMemorySettings settings, string updatedBy)
    {
        await UpdateSettingAsync("AI:RecentMessageWindow", settings.RecentMessageWindow.ToString(), updatedBy);
        await UpdateSettingAsync("AI:ConversationSummaryInterval", settings.ConversationSummaryInterval.ToString(), updatedBy);
        await UpdateSettingAsync("AI:MaxConversationSummaryLength", settings.MaxConversationSummaryLength.ToString(), updatedBy);
        await UpdateSettingAsync(
            "AI:EnableConversationSummary",
            settings.EnableConversationSummary.ToString().ToLowerInvariant(),
            updatedBy);
    }

    public async Task UpdateChatPoliciesAsync(IReadOnlyList<ChatPolicyItem> policies, string updatedBy)
    {
        foreach (var policy in policies)
        {
            if (string.IsNullOrWhiteSpace(policy.Key) || !policy.Key.StartsWith("AI:Policy:", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Invalid chat policy key: '{policy.Key}'");
            }

            await UpdateSettingAsync(policy.Key, policy.Value, updatedBy);
        }
    }

    public async Task<IReadOnlyList<ChatPolicyItem>> GetChatPoliciesAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _context.ApplicationSettings
            .Where(s => s.Category == "AI:Policy" || s.Key.StartsWith("AI:Policy:"))
            .OrderBy(s => s.Key)
            .ToListAsync(cancellationToken);

        return settings
            .Select(s => new ChatPolicyItem
            {
                Key = s.Key,
                Value = s.Value,
                Description = s.Description
            })
            .ToList();
    }

    public async Task<(AISettings AiSettings, IReadOnlyList<ChatPolicyItem> Policies)> GetChatMemoryApplicationSettingsAsync(
        CancellationToken cancellationToken = default)
    {
        var settings = await _context.ApplicationSettings
            .Where(s => s.Key.StartsWith("AI:"))
            .ToListAsync(cancellationToken);

        var values = settings.ToDictionary(s => s.Key, s => s.Value, StringComparer.OrdinalIgnoreCase);

        string? GetValue(string key) => values.TryGetValue(key, out var value) ? value : null;

        var aiSettings = new AISettings
        {
            MaxSearchResults = int.TryParse(GetValue("AI:MaxSearchResults"), out var searchResults) ? searchResults : 5,
            MaxContentLengthPerSource = int.TryParse(GetValue("AI:MaxContentLengthPerSource"), out var contentLength) ? contentLength : 800,
            MaxHistoryMessages = int.TryParse(GetValue("AI:MaxHistoryMessages"), out var historyMessages) ? historyMessages : 10,
            MaxTotalTokens = int.TryParse(GetValue("AI:MaxTotalTokens"), out var totalTokens) ? totalTokens : 7000,
            RecentMessageWindow = int.TryParse(GetValue("AI:RecentMessageWindow"), out var recentWindow) ? recentWindow : 4,
            ConversationSummaryInterval = int.TryParse(GetValue("AI:ConversationSummaryInterval"), out var summaryInterval) ? summaryInterval : 6,
            MaxConversationSummaryLength = int.TryParse(GetValue("AI:MaxConversationSummaryLength"), out var maxSummaryLen) ? maxSummaryLen : 1500,
            EnableConversationSummary = !bool.TryParse(GetValue("AI:EnableConversationSummary"), out var enableSummary) || enableSummary
        };

        var policies = settings
            .Where(s => s.Category == "AI:Policy" || s.Key.StartsWith("AI:Policy:", StringComparison.Ordinal))
            .OrderBy(s => s.Key)
            .Select(s => new ChatPolicyItem
            {
                Key = s.Key,
                Value = s.Value,
                Description = s.Description
            })
            .ToList();

        return (aiSettings, policies);
    }

    public async Task<MemorySettings> GetMemorySettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _context.ApplicationSettings
            .Where(s => s.Key.StartsWith("AI:Memory:"))
            .ToListAsync(cancellationToken);

        var values = settings.ToDictionary(s => s.Key, s => s.Value, StringComparer.OrdinalIgnoreCase);
        string? GetValue(string key) => values.TryGetValue(key, out var value) ? value : null;

        return new MemorySettings
        {
            EnableMemoryFormation = !bool.TryParse(GetValue("AI:Memory:EnableMemoryFormation"), out var enabled) || enabled,
            MinPromotionConfidence = double.TryParse(GetValue("AI:Memory:MinPromotionConfidence"), out var minConf) ? minConf : 0.75,
            ProvisionalConfidence = double.TryParse(GetValue("AI:Memory:ProvisionalConfidence"), out var provConf) ? provConf : 0.60,
            ActivationObservationCount = int.TryParse(GetValue("AI:Memory:ActivationObservationCount"), out var obs) ? obs : 2,
            MaxFactsPerTurn = int.TryParse(GetValue("AI:Memory:MaxFactsPerTurn"), out var maxFacts) ? maxFacts : 3,
            MaxEpisodesPerTurn = int.TryParse(GetValue("AI:Memory:MaxEpisodesPerTurn"), out var maxEps) ? maxEps : 2,
            MaxFactsRetrieved = int.TryParse(GetValue("AI:Memory:MaxFactsRetrieved"), out var maxFactsRet) ? maxFactsRet : 5,
            MaxEpisodesRetrieved = int.TryParse(GetValue("AI:Memory:MaxEpisodesRetrieved"), out var maxEpsRet) ? maxEpsRet : 3,
            IndexProvisionalMemories = bool.TryParse(GetValue("AI:Memory:IndexProvisionalMemories"), out var indexProv) && indexProv,
            EnableMemoryDecay = !bool.TryParse(GetValue("AI:Memory:EnableMemoryDecay"), out var decayEnabled) || decayEnabled,
            ProvisionalTtlDays = int.TryParse(GetValue("AI:Memory:ProvisionalTtlDays"), out var provTtl) ? provTtl : 30,
            ActiveFactTtlDays = int.TryParse(GetValue("AI:Memory:ActiveFactTtlDays"), out var factTtl) ? factTtl : 365,
            ActiveEpisodeTtlDays = int.TryParse(GetValue("AI:Memory:ActiveEpisodeTtlDays"), out var epTtl) ? epTtl : 180,
            DecayInactivityDays = int.TryParse(GetValue("AI:Memory:DecayInactivityDays"), out var inactivity) ? inactivity : 90
        };
    }

    public async Task UpdateMemorySettingsAsync(MemorySettings settings, string updatedBy)
    {
        await UpdateSettingAsync("AI:Memory:EnableMemoryFormation", settings.EnableMemoryFormation.ToString().ToLowerInvariant(), updatedBy);
        await UpdateSettingAsync("AI:Memory:MinPromotionConfidence", settings.MinPromotionConfidence.ToString("F2", System.Globalization.CultureInfo.InvariantCulture), updatedBy);
        await UpdateSettingAsync("AI:Memory:ProvisionalConfidence", settings.ProvisionalConfidence.ToString("F2", System.Globalization.CultureInfo.InvariantCulture), updatedBy);
        await UpdateSettingAsync("AI:Memory:ActivationObservationCount", settings.ActivationObservationCount.ToString(), updatedBy);
        await UpdateSettingAsync("AI:Memory:MaxFactsPerTurn", settings.MaxFactsPerTurn.ToString(), updatedBy);
        await UpdateSettingAsync("AI:Memory:MaxEpisodesPerTurn", settings.MaxEpisodesPerTurn.ToString(), updatedBy);
        await UpdateSettingAsync("AI:Memory:MaxFactsRetrieved", settings.MaxFactsRetrieved.ToString(), updatedBy);
        await UpdateSettingAsync("AI:Memory:MaxEpisodesRetrieved", settings.MaxEpisodesRetrieved.ToString(), updatedBy);
        await UpdateSettingAsync("AI:Memory:IndexProvisionalMemories", settings.IndexProvisionalMemories.ToString().ToLowerInvariant(), updatedBy);
        await UpdateSettingAsync("AI:Memory:EnableMemoryDecay", settings.EnableMemoryDecay.ToString().ToLowerInvariant(), updatedBy);
        await UpdateSettingAsync("AI:Memory:ProvisionalTtlDays", settings.ProvisionalTtlDays.ToString(), updatedBy);
        await UpdateSettingAsync("AI:Memory:ActiveFactTtlDays", settings.ActiveFactTtlDays.ToString(), updatedBy);
        await UpdateSettingAsync("AI:Memory:ActiveEpisodeTtlDays", settings.ActiveEpisodeTtlDays.ToString(), updatedBy);
        await UpdateSettingAsync("AI:Memory:DecayInactivityDays", settings.DecayInactivityDays.ToString(), updatedBy);
    }

    public async Task<PromptBudgetSettings> GetPromptBudgetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _context.ApplicationSettings
            .Where(s => s.Key.StartsWith("AI:EnablePromptBudget")
                        || s.Key.StartsWith("AI:PromptBudget:"))
            .ToListAsync(cancellationToken);

        var values = settings.ToDictionary(s => s.Key, s => s.Value, StringComparer.OrdinalIgnoreCase);
        string? GetValue(string key) => values.TryGetValue(key, out var value) ? value : null;

        return new PromptBudgetSettings
        {
            EnablePromptBudget = bool.TryParse(GetValue("AI:EnablePromptBudget"), out var enabled) && enabled,
            SystemReserve = int.TryParse(GetValue("AI:PromptBudget:SystemReserve"), out var system) ? system : 400,
            PoliciesReserve = int.TryParse(GetValue("AI:PromptBudget:PoliciesReserve"), out var policies) ? policies : 300,
            PreferencesReserve = int.TryParse(GetValue("AI:PromptBudget:PreferencesReserve"), out var prefs) ? prefs : 200,
            QueryReserve = int.TryParse(GetValue("AI:PromptBudget:QueryReserve"), out var query) ? query : 200,
            SummaryReserve = int.TryParse(GetValue("AI:PromptBudget:SummaryReserve"), out var summary) ? summary : 400,
            FactsReserve = int.TryParse(GetValue("AI:PromptBudget:FactsReserve"), out var facts) ? facts : 300,
            EpisodesReserve = int.TryParse(GetValue("AI:PromptBudget:EpisodesReserve"), out var episodes) ? episodes : 300,
            OnboardingReserve = int.TryParse(GetValue("AI:PromptBudget:OnboardingReserve"), out var onboarding) ? onboarding : 500,
            HistoryReserve = int.TryParse(GetValue("AI:PromptBudget:HistoryReserve"), out var history) ? history : 800
        };
    }

    public async Task UpdatePromptBudgetSettingsAsync(PromptBudgetSettings settings, string updatedBy)
    {
        await UpdateSettingAsync("AI:EnablePromptBudget", settings.EnablePromptBudget.ToString().ToLowerInvariant(), updatedBy);
        await UpdateSettingAsync("AI:PromptBudget:SystemReserve", settings.SystemReserve.ToString(), updatedBy);
        await UpdateSettingAsync("AI:PromptBudget:PoliciesReserve", settings.PoliciesReserve.ToString(), updatedBy);
        await UpdateSettingAsync("AI:PromptBudget:PreferencesReserve", settings.PreferencesReserve.ToString(), updatedBy);
        await UpdateSettingAsync("AI:PromptBudget:QueryReserve", settings.QueryReserve.ToString(), updatedBy);
        await UpdateSettingAsync("AI:PromptBudget:SummaryReserve", settings.SummaryReserve.ToString(), updatedBy);
        await UpdateSettingAsync("AI:PromptBudget:FactsReserve", settings.FactsReserve.ToString(), updatedBy);
        await UpdateSettingAsync("AI:PromptBudget:EpisodesReserve", settings.EpisodesReserve.ToString(), updatedBy);
        await UpdateSettingAsync("AI:PromptBudget:OnboardingReserve", settings.OnboardingReserve.ToString(), updatedBy);
        await UpdateSettingAsync("AI:PromptBudget:HistoryReserve", settings.HistoryReserve.ToString(), updatedBy);
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
    Task UpsertSettingAsync(string key, string value, string category, string description, string updatedBy);
    Task<AISettings> GetAISettingsAsync();
    Task UpdateAISettingsAsync(AISettings settings, string updatedBy);
    Task<ChatMemorySettings> GetChatMemorySettingsAsync();
    Task UpdateChatMemorySettingsAsync(ChatMemorySettings settings, string updatedBy);
    Task<IReadOnlyList<ChatPolicyItem>> GetChatPoliciesAsync(CancellationToken cancellationToken = default);
    Task<(AISettings AiSettings, IReadOnlyList<ChatPolicyItem> Policies)> GetChatMemoryApplicationSettingsAsync(
        CancellationToken cancellationToken = default);
    Task<MemorySettings> GetMemorySettingsAsync(CancellationToken cancellationToken = default);
    Task UpdateMemorySettingsAsync(MemorySettings settings, string updatedBy);
    Task<PromptBudgetSettings> GetPromptBudgetSettingsAsync(CancellationToken cancellationToken = default);
    Task UpdatePromptBudgetSettingsAsync(PromptBudgetSettings settings, string updatedBy);
    Task UpdateChatPoliciesAsync(IReadOnlyList<ChatPolicyItem> policies, string updatedBy);
}

public class ChatMemorySettings
{
    public int RecentMessageWindow { get; set; }
    public int ConversationSummaryInterval { get; set; }
    public int MaxConversationSummaryLength { get; set; }
    public bool EnableConversationSummary { get; set; }
}

public class AISettings
{
    public int MaxSearchResults { get; set; }
    public int MaxContentLengthPerSource { get; set; }
    public int MaxHistoryMessages { get; set; }
    public int MaxTotalTokens { get; set; }
    public int RecentMessageWindow { get; set; }
    public int ConversationSummaryInterval { get; set; }
    public int MaxConversationSummaryLength { get; set; }
    public bool EnableConversationSummary { get; set; }
}
