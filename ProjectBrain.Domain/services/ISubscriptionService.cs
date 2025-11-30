namespace ProjectBrain.Domain;

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
    Task<SubscriptionSettings> GetSubscriptionSettingsAsync();
    Task UpdateSubscriptionSettingsAsync(bool enableUsers, bool enableCoaches, string updatedBy);
}

