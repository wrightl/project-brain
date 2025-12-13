namespace ProjectBrain.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class SubscriptionService : ISubscriptionService
{
    private readonly AppDbContext _context;
    private readonly IStripeService _stripeService;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(AppDbContext context, IStripeService stripeService, ILogger<SubscriptionService> logger)
    {
        _context = context;
        _stripeService = stripeService;
        _logger = logger;
    }

    public async Task<UserSubscription?> GetUserSubscriptionAsync(string userId, string userType)
    {
        return await _context.UserSubscriptions
            .Include(us => us.Tier)
            .Where(us => us.UserId == userId && us.UserType == userType)
            .OrderByDescending(us => us.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<string> GetUserTierAsync(string userId, string userType)
    {
        // Check if user is excluded
        var isExcluded = await _context.SubscriptionExclusions
            .AnyAsync(se => se.UserId == userId && se.UserType == userType);

        if (isExcluded)
        {
            return "Free"; // Excluded users get Free tier access
        }

        // Check if subscription system is enabled for this user type
        var settings = await GetSubscriptionSettingsAsync();
        if (userType == "user" && !settings.EnableUserSubscriptions)
        {
            return "Free"; // If disabled, everyone gets Free tier
        }
        if (userType == "coach" && !settings.EnableCoachSubscriptions)
        {
            return "Free";
        }

        // Get active subscription
        var subscription = await GetUserSubscriptionAsync(userId, userType);

        if (subscription != null && (subscription.Status == "active" || subscription.Status == "trialing"))
        {
            return subscription.Tier?.Name ?? "Free";
        }

        return "Free"; // Default to Free tier
    }

    public async Task<string> CreateCheckoutSessionAsync(string userId, string userType, string tier, bool isAnnual)
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
                subscription.StripeCustomerId = customerId;
                subscription.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create a placeholder subscription record to store the customer ID
                var tierEntity = await _context.SubscriptionTiers
                    .FirstOrDefaultAsync(t => t.Name == "Free" && t.UserType == userType);

                if (tierEntity != null)
                {
                    subscription = new UserSubscription
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        UserType = userType,
                        TierId = tierEntity.Id,
                        StripeCustomerId = customerId,
                        Status = "incomplete",
                        CurrentPeriodStart = DateTime.UtcNow,
                        CurrentPeriodEnd = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.UserSubscriptions.Add(subscription);
                }
            }
            await _context.SaveChangesAsync();
        }

        // Create checkout session
        var checkoutUrl = await _stripeService.CreateCheckoutSessionAsync(userId, userType, tier, isAnnual, customerId);
        return checkoutUrl;
    }

    public async Task UpdateSubscriptionFromStripeAsync(string stripeSubscriptionId)
    {
        var subscription = await _context.UserSubscriptions
            .Include(us => us.Tier)
            .FirstOrDefaultAsync(us => us.StripeSubscriptionId == stripeSubscriptionId);

        if (subscription == null)
        {
            _logger.LogWarning("Subscription not found for Stripe subscription ID {StripeSubscriptionId}", stripeSubscriptionId);
            return;
        }

        // Fetch subscription details from Stripe
        var stripeSubscription = await _stripeService.GetSubscriptionAsync(stripeSubscriptionId);

        // Update subscription status
        subscription.Status = stripeSubscription.Status switch
        {
            "active" => "active",
            "trialing" => "trialing",
            "past_due" => "past_due",
            "canceled" => "canceled",
            "unpaid" => "expired",
            _ => subscription.Status
        };

        subscription.TrialEndsAt = stripeSubscription.TrialEnd;
        subscription.CurrentPeriodStart = stripeSubscription.CurrentPeriodStart;
        subscription.CurrentPeriodEnd = stripeSubscription.CurrentPeriodEnd;
        subscription.StripePriceId = stripeSubscription.PriceId;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated subscription {SubscriptionId} from Stripe", subscription.Id);
    }

    public async Task CancelSubscriptionAsync(string userId, string userType)
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

        subscription.Status = "canceled";
        subscription.CanceledAt = DateTime.UtcNow;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Subscription {SubscriptionId} canceled for user {UserId}", subscription.Id, userId);
    }

    public async Task StartTrialAsync(string userId, string userType, string tier)
    {
        // Get tier ID
        var tierEntity = await _context.SubscriptionTiers
            .FirstOrDefaultAsync(t => t.Name == tier && t.UserType == userType);

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
            UserType = userType,
            TierId = tierEntity.Id,
            Status = "trialing",
            TrialEndsAt = trialEndsAt,
            CurrentPeriodStart = periodStart,
            CurrentPeriodEnd = periodEnd,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.UserSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Started 7-day trial for user {UserId}, type {UserType}, tier {Tier}", userId, userType, tier);
    }

    public async Task<bool> IsSubscriptionRequiredAsync(string userId, string userType)
    {
        // Check if user is excluded
        var isExcluded = await _context.SubscriptionExclusions
            .AnyAsync(se => se.UserId == userId && se.UserType == userType);

        if (isExcluded)
        {
            return false; // Excluded users don't need subscription
        }

        // Check if subscription system is enabled
        var settings = await GetSubscriptionSettingsAsync();
        if (userType == "user" && !settings.EnableUserSubscriptions)
        {
            return false;
        }
        if (userType == "coach" && !settings.EnableCoachSubscriptions)
        {
            return false;
        }

        return true; // Subscription is required
    }

    public async Task ExcludeUserFromSubscriptionAsync(string userId, string userType, string excludedBy, string? notes)
    {
        // Check if exclusion already exists
        var existing = await _context.SubscriptionExclusions
            .FirstOrDefaultAsync(se => se.UserId == userId && se.UserType == userType);

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
                UserType = userType,
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

    public async Task RemoveExclusionAsync(string userId, string userType)
    {
        var exclusion = await _context.SubscriptionExclusions
            .FirstOrDefaultAsync(se => se.UserId == userId && se.UserType == userType);

        if (exclusion != null)
        {
            _context.SubscriptionExclusions.Remove(exclusion);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Exclusion removed for user {UserId}, type {UserType}", userId, userType);
        }
    }

    public async Task<bool> IsUserExcludedAsync(string userId, string userType)
    {
        return await _context.SubscriptionExclusions
            .AnyAsync(se => se.UserId == userId && se.UserType == userType);
    }

    public async Task<SubscriptionSettings> GetSubscriptionSettingsAsync()
    {
        var settings = await _context.SubscriptionSettings.FirstOrDefaultAsync();

        if (settings == null)
        {
            // Create default settings if none exist
            settings = new SubscriptionSettings
            {
                Id = 1,
                EnableUserSubscriptions = true,
                EnableCoachSubscriptions = true,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = "system"
            };
            _context.SubscriptionSettings.Add(settings);
            await _context.SaveChangesAsync();
        }

        return settings;
    }

    public async Task UpdateSubscriptionSettingsAsync(bool enableUsers, bool enableCoaches, string updatedBy)
    {
        var settings = await GetSubscriptionSettingsAsync();
        settings.EnableUserSubscriptions = enableUsers;
        settings.EnableCoachSubscriptions = enableCoaches;
        settings.UpdatedBy = updatedBy;
        settings.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Subscription settings updated by {UpdatedBy}: Users={EnableUsers}, Coaches={EnableCoaches}",
            updatedBy, enableUsers, enableCoaches);
    }
}

public interface ISubscriptionService
{
    Task<UserSubscription?> GetUserSubscriptionAsync(string userId, string userType);
    Task<string> GetUserTierAsync(string userId, string userType);
    Task<string> CreateCheckoutSessionAsync(string userId, string userType, string tier, bool isAnnual);
    Task UpdateSubscriptionFromStripeAsync(string stripeSubscriptionId);
    Task CancelSubscriptionAsync(string userId, string userType);
    Task StartTrialAsync(string userId, string userType, string tier);
    Task<bool> IsSubscriptionRequiredAsync(string userId, string userType);
    Task ExcludeUserFromSubscriptionAsync(string userId, string userType, string excludedBy, string? notes);
    Task RemoveExclusionAsync(string userId, string userType);
    Task<bool> IsUserExcludedAsync(string userId, string userType);
    Task<SubscriptionSettings> GetSubscriptionSettingsAsync();
    Task UpdateSubscriptionSettingsAsync(bool enableUsers, bool enableCoaches, string updatedBy);
}