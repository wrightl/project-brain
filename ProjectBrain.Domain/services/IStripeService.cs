namespace ProjectBrain.Domain;

public interface IStripeService
{
    Task<string> CreateCustomerAsync(string userId, string email, string name);
    Task<string> CreateCheckoutSessionAsync(string userId, string userType, string tier, bool isAnnual, string? customerId = null);
    Task<StripeSubscriptionInfo> GetSubscriptionAsync(string stripeSubscriptionId);
    Task CancelSubscriptionAsync(string stripeSubscriptionId);
}

public class StripeSubscriptionInfo
{
    public required string Id { get; set; }
    public required string Status { get; set; }
    public required string CustomerId { get; set; }
    public DateTime? TrialEnd { get; set; }
    public DateTime CurrentPeriodStart { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    public string? PriceId { get; set; }
}

