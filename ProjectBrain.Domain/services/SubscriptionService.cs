namespace ProjectBrain.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectBrain.Domain.Caching;
using ProjectBrain.Domain.Repositories;
using ProjectBrain.Domain.UnitOfWork;

public enum UserType
{
    User,
    Coach,
    Admin
}

public class SubscriptionService : ISubscriptionService
{
    private readonly IUserSubscriptionRepository _repository;
    private readonly AppDbContext _context;
    private readonly IStripeService _stripeService;
    private readonly ILogger<SubscriptionService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private const string TierCacheKeyPrefix = "subscription:tier:";
    private const string SettingsCacheKey = "subscription:settings";
    private static readonly TimeSpan TierCacheExpiration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan SettingsCacheExpiration = TimeSpan.FromMinutes(30);
    private const string EnableUserSubscriptionsKey = "Subscription:EnableUserSubscriptions";
    private const string EnableCoachSubscriptionsKey = "Subscription:EnableCoachSubscriptions";
    private const string SubscriptionSettingsCategory = "Subscription";

    public SubscriptionService(
        IUserSubscriptionRepository repository,
        AppDbContext context,
        IStripeService stripeService,
        ILogger<SubscriptionService> logger,
        IUnitOfWork unitOfWork,
        ICacheService cache)
    {
        _repository = repository;
        _context = context;
        _stripeService = stripeService;
        _logger = logger;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<UserSubscription?> GetUserSubscriptionAsync(string userId, UserType userType)
    {
        return await _repository.GetLatestForUserAsync(userId, userType);
    }

    public async Task<string> GetUserTierAsync(string userId, UserType userType)
    {
        // Try cache first
        var cacheKey = $"{TierCacheKeyPrefix}{userId}:{userType}";
        var cachedTier = await _cache.GetAsync<string>(cacheKey);
        if (cachedTier != null)
        {
            return cachedTier;
        }

        // Check if user is excluded
        var isExcluded = await _context.SubscriptionExclusions
            .AnyAsync(se => se.UserId == userId && se.UserType == userType.ToString());

        if (isExcluded)
        {
            var tier = "Free"; // Excluded users get Free tier access
            await _cache.SetAsync(cacheKey, tier, TierCacheExpiration);
            return tier;
        }

        // Check if subscription system is enabled for this user type
        var settings = await GetSubscriptionSettingsAsync();
        if (userType == UserType.User && !settings.EnableUserSubscriptions)
        {
            var tier = "Free"; // If disabled, everyone gets Free tier
            await _cache.SetAsync(cacheKey, tier, TierCacheExpiration);
            return tier;
        }
        if (userType == UserType.Coach && !settings.EnableCoachSubscriptions)
        {
            var tier = "Free";
            await _cache.SetAsync(cacheKey, tier, TierCacheExpiration);
            return tier;
        }

        // Get active subscription
        var subscription = await GetUserSubscriptionAsync(userId, userType);

        string tierResult;
        if (subscription != null && (subscription.Status == "active" || subscription.Status == "trialing"))
        {
            tierResult = subscription.Tier?.Name ?? "Free";
        }
        else
        {
            tierResult = "Free"; // Default to Free tier
        }

        // Cache the result
        await _cache.SetAsync(cacheKey, tierResult, TierCacheExpiration);
        return tierResult;
    }

    public async Task<string> CreateCheckoutSessionAsync(string userId, UserType userType, string tier, bool isAnnual)
    {
        // Get or create Stripe customer
        var subscription = await GetUserSubscriptionAsync(userId, userType);
        string? customerId = subscription?.StripeCustomerId;

        if (string.IsNullOrEmpty(customerId))
        {
            // Get user details to create customer
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new Exception($"User {userId} not found");
            }

            customerId = await _stripeService.CreateCustomerAsync(userId, user.Email, user.FullName);

            // Save customer ID to subscription if it exists, or create new subscription record
            if (subscription != null)
            {
                // Get tracked entity for update
                var trackedSubscription = await _context.UserSubscriptions
                    .FirstOrDefaultAsync(us => us.Id == subscription.Id);
                if (trackedSubscription != null)
                {
                    trackedSubscription.StripeCustomerId = customerId;
                    trackedSubscription.UpdatedAt = DateTime.UtcNow;
                    _repository.Update(trackedSubscription);
                }
            }
            else
            {
                // Create a placeholder subscription record to store the customer ID
                var tierEntity = await _context.SubscriptionTiers
                    .FirstOrDefaultAsync(t => t.Name == "Free" && t.UserType == userType.ToString());

                if (tierEntity != null)
                {
                    subscription = new UserSubscription
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        UserType = userType.ToString(),
                        TierId = tierEntity.Id,
                        StripeCustomerId = customerId,
                        Status = "incomplete",
                        CurrentPeriodStart = DateTime.UtcNow,
                        CurrentPeriodEnd = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _repository.Add(subscription);
                }
            }
            await _unitOfWork.SaveChangesAsync();
        }

        // Create checkout session
        var checkoutUrl = await _stripeService.CreateCheckoutSessionAsync(userId, userType, tier, isAnnual, customerId);
        return checkoutUrl;
    }

    public async Task UpdateSubscriptionFromStripeAsync(string stripeSubscriptionId)
    {
        var subscription = await _repository.GetByStripeSubscriptionIdAsync(stripeSubscriptionId);
        if (subscription == null)
        {
            _logger.LogWarning("Subscription not found for Stripe subscription ID {StripeSubscriptionId}", stripeSubscriptionId);
            return;
        }

        // Get tracked entity for update
        var trackedSubscription = await _context.UserSubscriptions
            .FirstOrDefaultAsync(us => us.Id == subscription.Id);
        if (trackedSubscription == null)
        {
            _logger.LogWarning("Tracked subscription not found for ID {SubscriptionId}", subscription.Id);
            return;
        }

        // Fetch subscription details from Stripe
        var stripeSubscription = await _stripeService.GetSubscriptionAsync(stripeSubscriptionId);

        // Update subscription status
        trackedSubscription.Status = stripeSubscription.Status switch
        {
            "active" => "active",
            "trialing" => "trialing",
            "past_due" => "past_due",
            "canceled" => "canceled",
            "unpaid" => "expired",
            _ => trackedSubscription.Status
        };

        trackedSubscription.TrialEndsAt = stripeSubscription.TrialEnd;
        trackedSubscription.CurrentPeriodStart = stripeSubscription.CurrentPeriodStart;
        trackedSubscription.CurrentPeriodEnd = stripeSubscription.CurrentPeriodEnd;
        trackedSubscription.StripePriceId = stripeSubscription.PriceId;
        trackedSubscription.UpdatedAt = DateTime.UtcNow;

        _repository.Update(trackedSubscription);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Updated subscription {SubscriptionId} from Stripe", trackedSubscription.Id);
    }

    public async Task CancelSubscriptionAsync(string userId, UserType userType)
    {
        var subscription = await GetUserSubscriptionAsync(userId, userType);

        if (subscription == null)
        {
            _logger.LogWarning("No subscription found for user {UserId}, type {UserType}", userId, userType);
            return;
        }

        // Cancel in Stripe if subscription ID exists
        if (!string.IsNullOrEmpty(subscription.StripeSubscriptionId))
        {
            await _stripeService.CancelSubscriptionAsync(subscription.StripeSubscriptionId);
        }

        // Get tracked entity for update
        var trackedSubscription = await _context.UserSubscriptions
            .FirstOrDefaultAsync(us => us.Id == subscription.Id);
        if (trackedSubscription != null)
        {
            trackedSubscription.Status = "canceled";
            trackedSubscription.CanceledAt = DateTime.UtcNow;
            trackedSubscription.UpdatedAt = DateTime.UtcNow;
            _repository.Update(trackedSubscription);
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Subscription {SubscriptionId} canceled for user {UserId}", subscription.Id, userId);
    }

    public async Task StartTrialAsync(string userId, UserType userType, string tier)
    {
        // Get tier ID
        var tierEntity = await _context.SubscriptionTiers
            .FirstOrDefaultAsync(t => t.Name == tier && t.UserType == userType.ToString());

        if (tierEntity == null)
        {
            throw new Exception($"Tier {tier} not found for user type {userType}");
        }

        // TODO: Check if user has already had a trial. The Status would be 'Expired' and the TrialEndsAt would be in the past.
        // Check if user already has an active subscription
        var existingSubscription = await GetUserSubscriptionAsync(userId, userType);
        if (existingSubscription != null && (existingSubscription.Status == "active" || existingSubscription.Status == "trialing"))
        {
            throw new Exception("User already has an active subscription");
        }

        var trialEndsAt = DateTime.UtcNow.AddDays(7);
        var periodStart = DateTime.UtcNow;
        var periodEnd = trialEndsAt;

        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            UserType = userType.ToString(),
            TierId = tierEntity.Id,
            Status = "trialing",
            TrialEndsAt = trialEndsAt,
            CurrentPeriodStart = periodStart,
            CurrentPeriodEnd = periodEnd,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _repository.Add(subscription);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Started 7-day trial for user {UserId}, type {UserType}, tier {Tier}", userId, userType, tier);
    }

    public async Task<bool> IsSubscriptionRequiredAsync(string userId, UserType userType)
    {
        // Check if user is excluded
        var isExcluded = await _context.SubscriptionExclusions
            .AnyAsync(se => se.UserId == userId && se.UserType == userType.ToString());

        if (isExcluded)
        {
            return false; // Excluded users don't need subscription
        }

        // Check if subscription system is enabled
        var settings = await GetSubscriptionSettingsAsync();
        if (userType == UserType.User && !settings.EnableUserSubscriptions)
        {
            return false;
        }
        if (userType == UserType.Coach && !settings.EnableCoachSubscriptions)
        {
            return false;
        }

        return true; // Subscription is required
    }

    public async Task ExcludeUserFromSubscriptionAsync(string userId, UserType userType, string excludedBy, string? notes)
    {
        // Check if exclusion already exists
        var existing = await _context.SubscriptionExclusions
            .FirstOrDefaultAsync(se => se.UserId == userId && se.UserType == userType.ToString());

        if (existing != null)
        {
            existing.ExcludedBy = excludedBy;
            existing.ExcludedAt = DateTime.UtcNow;
            existing.Notes = notes;
        }
        else
        {
            var exclusion = new SubscriptionExclusion
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserType = userType.ToString(),
                ExcludedBy = excludedBy,
                ExcludedAt = DateTime.UtcNow,
                Notes = notes
            };
            _context.SubscriptionExclusions.Add(exclusion);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("User {UserId}, type {UserType} excluded from subscription requirements by {ExcludedBy}",
            userId, userType, excludedBy);
    }

    public async Task RemoveExclusionAsync(string userId, UserType userType)
    {
        var exclusion = await _context.SubscriptionExclusions
            .FirstOrDefaultAsync(se => se.UserId == userId && se.UserType == userType.ToString());

        if (exclusion != null)
        {
            _context.SubscriptionExclusions.Remove(exclusion);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Exclusion removed for user {UserId}, type {UserType}", userId, userType);
        }
    }

    public async Task<bool> IsUserExcludedAsync(string userId, UserType userType)
    {
        return await _repository.IsUserExcludedAsync(userId, userType);
    }

    public async Task<SubscriptionSettingsInfo> GetSubscriptionSettingsAsync()
    {
        // Try cache first
        var cachedSettings = await _cache.GetAsync<SubscriptionSettingsInfo>(SettingsCacheKey);
        if (cachedSettings != null)
        {
            return cachedSettings;
        }

        var settingsRows = await _context.ApplicationSettings
            .AsNoTracking()
            .Where(s => s.Key == EnableUserSubscriptionsKey || s.Key == EnableCoachSubscriptionsKey)
            .ToListAsync();

        var enableUserSubscriptions = true;
        var enableCoachSubscriptions = true;

        foreach (var row in settingsRows)
        {
            if (row.Key == EnableUserSubscriptionsKey)
            {
                enableUserSubscriptions = bool.TryParse(row.Value, out var parsed) ? parsed : true;
            }
            else if (row.Key == EnableCoachSubscriptionsKey)
            {
                enableCoachSubscriptions = bool.TryParse(row.Value, out var parsed) ? parsed : true;
            }
        }

        var latest = settingsRows.OrderByDescending(s => s.UpdatedAt).FirstOrDefault();
        var settings = new SubscriptionSettingsInfo
        {
            EnableUserSubscriptions = enableUserSubscriptions,
            EnableCoachSubscriptions = enableCoachSubscriptions,
            UpdatedAt = latest?.UpdatedAt ?? DateTime.UtcNow,
            UpdatedBy = latest?.UpdatedBy ?? "system"
        };

        // Cache the settings
        await _cache.SetAsync(SettingsCacheKey, settings, SettingsCacheExpiration);
        return settings;
    }

    public async Task UpdateSubscriptionSettingsAsync(bool enableUsers, bool enableCoaches, string updatedBy)
    {
        var settings = await GetSubscriptionSettingsAsync();
        settings.EnableUserSubscriptions = enableUsers;
        settings.EnableCoachSubscriptions = enableCoaches;
        settings.UpdatedBy = updatedBy;
        settings.UpdatedAt = DateTime.UtcNow;

        await UpsertApplicationSettingAsync(
            key: EnableUserSubscriptionsKey,
            value: enableUsers.ToString().ToLowerInvariant(),
            category: SubscriptionSettingsCategory,
            description: "Enable/disable subscriptions for regular users",
            updatedBy: updatedBy,
            updatedAt: settings.UpdatedAt);

        await UpsertApplicationSettingAsync(
            key: EnableCoachSubscriptionsKey,
            value: enableCoaches.ToString().ToLowerInvariant(),
            category: SubscriptionSettingsCategory,
            description: "Enable/disable subscriptions for coaches",
            updatedBy: updatedBy,
            updatedAt: settings.UpdatedAt);

        await _unitOfWork.SaveChangesAsync();

        // Invalidate cache
        await _cache.RemoveAsync(SettingsCacheKey);

        _logger.LogInformation("Subscription settings updated by {UpdatedBy}: Users={EnableUsers}, Coaches={EnableCoaches}",
            updatedBy, enableUsers, enableCoaches);
    }

    private async Task UpsertApplicationSettingAsync(
        string key,
        string value,
        string category,
        string description,
        string updatedBy,
        DateTime updatedAt)
    {
        var existing = await _context.ApplicationSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (existing == null)
        {
            _context.ApplicationSettings.Add(new ApplicationSetting
            {
                Key = key,
                Value = value,
                Category = category,
                Description = description,
                UpdatedAt = updatedAt,
                UpdatedBy = updatedBy
            });
            return;
        }

        existing.Value = value;
        existing.Category ??= category;
        existing.Description ??= description;
        existing.UpdatedBy = updatedBy;
        existing.UpdatedAt = updatedAt;
    }
}

public interface ISubscriptionService
{
    Task<UserSubscription?> GetUserSubscriptionAsync(string userId, UserType userType);
    Task<string> GetUserTierAsync(string userId, UserType userType);
    Task<string> CreateCheckoutSessionAsync(string userId, UserType userType, string tier, bool isAnnual);
    Task UpdateSubscriptionFromStripeAsync(string stripeSubscriptionId);
    Task CancelSubscriptionAsync(string userId, UserType userType);
    Task StartTrialAsync(string userId, UserType userType, string tier);
    Task<bool> IsSubscriptionRequiredAsync(string userId, UserType userType);
    Task ExcludeUserFromSubscriptionAsync(string userId, UserType userType, string excludedBy, string? notes);
    Task RemoveExclusionAsync(string userId, UserType userType);
    Task<bool> IsUserExcludedAsync(string userId, UserType userType);
    Task<SubscriptionSettingsInfo> GetSubscriptionSettingsAsync();
    Task UpdateSubscriptionSettingsAsync(bool enableUsers, bool enableCoaches, string updatedBy);
}

public class SubscriptionSettingsInfo
{
    public bool EnableUserSubscriptions { get; set; } = true;
    public bool EnableCoachSubscriptions { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string UpdatedBy { get; set; } = "system";
}