namespace ProjectBrain.Domain;

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;

public class StripeService : IStripeService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeService> _logger;
    private readonly string _secretKey;

    public StripeService(IConfiguration configuration, ILogger<StripeService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _secretKey = _configuration["Stripe:SecretKey"] ?? throw new InvalidOperationException("Stripe:SecretKey is not configured");

        // Initialize Stripe API key
        StripeConfiguration.ApiKey = _secretKey;
    }

    public async Task<string> CreateCustomerAsync(string userId, string email, string name)
    {
        try
        {
            var options = new CustomerCreateOptions
            {
                Email = email,
                Name = name,
                Metadata = new Dictionary<string, string>
                {
                    { "userId", userId }
                }
            };

            var service = new CustomerService();
            var customer = await service.CreateAsync(options);

            _logger.LogInformation("Created Stripe customer {CustomerId} for user {UserId}", customer.Id, userId);
            return customer.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Stripe customer for user {UserId}", userId);
            throw;
        }
    }

    public async Task<string> CreateCheckoutSessionAsync(string userId, UserType userType, string tier, bool isAnnual, string? customerId = null, string? baseUrl = null)
    {
        try
        {
            // Validate required configuration
            if (string.IsNullOrEmpty(_secretKey))
            {
                throw new InvalidOperationException("Stripe:SecretKey is not configured. Please configure Stripe credentials in appsettings.json");
            }

            var priceKey = $"{userType}{tier}{(isAnnual ? "Annual" : "Monthly")}";
            var priceId = _configuration[$"Stripe:PriceIds:{priceKey}"];

            if (string.IsNullOrEmpty(priceId))
            {
                throw new InvalidOperationException(
                    $"Stripe price ID not found for {priceKey}. " +
                    $"Please configure Stripe:PriceIds:{priceKey} in appsettings.json. " +
                    $"You need to create a product and price in Stripe Dashboard and add the price ID to configuration.");
            }

            // Set URLs based on userType
            var successPath = userType == UserType.Coach
                ? "/app/coach/subscription/success?session_id={CHECKOUT_SESSION_ID}"
                : "/app/user/subscription/success?session_id={CHECKOUT_SESSION_ID}";
            var cancelPath = userType == UserType.Coach
                ? "/app/coach/subscription/cancel"
                : "/app/user/subscription/cancel";

            var successUrl = $"{baseUrl}{successPath}";
            var cancelUrl = $"{baseUrl}{cancelPath}";

            var trialPeriodDays = tier == "Pro" ? 7 : (int?)null;

            var options = new Stripe.Checkout.SessionCreateOptions
            {
                Customer = customerId,
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<Stripe.Checkout.SessionLineItemOptions>
                {
                    new Stripe.Checkout.SessionLineItemOptions
                    {
                        Price = priceId,
                        Quantity = 1
                    }
                },
                Mode = "subscription",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    { "userId", userId },
                    { "userType", userType.ToString() },
                    { "tier", tier }
                },
                SubscriptionData = new Stripe.Checkout.SessionSubscriptionDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        { "userId", userId },
                        { "userType", userType.ToString() },
                        { "tier", tier }
                    }
                }
            };

            if (trialPeriodDays.HasValue)
            {
                options.SubscriptionData = options.SubscriptionData ?? new Stripe.Checkout.SessionSubscriptionDataOptions();
                options.SubscriptionData.TrialPeriodDays = trialPeriodDays.Value;
            }

            var service = new Stripe.Checkout.SessionService();
            var session = await service.CreateAsync(options);

            _logger.LogInformation("Created Stripe checkout session {SessionId} for user {UserId}, tier {Tier}",
                session.Id, userId, tier);
            return session.Url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Stripe checkout session for user {UserId}", userId);
            throw;
        }
    }

    public async Task<StripeSubscriptionInfo> GetSubscriptionAsync(string stripeSubscriptionId)
    {
        try
        {
            var service = new Stripe.SubscriptionService();
            var subscription = await service.GetAsync(stripeSubscriptionId);

            // Get period dates - Stripe.NET property names may vary by version
            DateTime periodStart = subscription.Created;
            DateTime periodEnd = subscription.Created.AddMonths(1);

            // Try to get CurrentPeriodStart and CurrentPeriodEnd using reflection
            var periodStartProp = typeof(Stripe.Subscription).GetProperty("CurrentPeriodStart");
            var periodEndProp = typeof(Stripe.Subscription).GetProperty("CurrentPeriodEnd");

            if (periodStartProp != null)
            {
                var startValue = periodStartProp.GetValue(subscription);
                if (startValue is DateTimeOffset startOffset)
                {
                    periodStart = startOffset.UtcDateTime;
                }
                else if (startValue is long startUnix)
                {
                    periodStart = DateTimeOffset.FromUnixTimeSeconds(startUnix).UtcDateTime;
                }
            }

            if (periodEndProp != null)
            {
                var endValue = periodEndProp.GetValue(subscription);
                if (endValue is DateTimeOffset endOffset)
                {
                    periodEnd = endOffset.UtcDateTime;
                }
                else if (endValue is long endUnix)
                {
                    periodEnd = DateTimeOffset.FromUnixTimeSeconds(endUnix).UtcDateTime;
                }
            }

            // Extract metadata from subscription
            var metadata = new Dictionary<string, string>();
            if (subscription.Metadata != null)
            {
                foreach (var kvp in subscription.Metadata)
                {
                    metadata[kvp.Key] = kvp.Value;
                }
            }

            return new StripeSubscriptionInfo
            {
                Id = subscription.Id,
                Status = subscription.Status,
                CustomerId = subscription.CustomerId,
                TrialEnd = subscription.TrialEnd,
                CurrentPeriodStart = periodStart,
                CurrentPeriodEnd = periodEnd,
                PriceId = subscription.Items?.Data?.FirstOrDefault()?.Price?.Id,
                Metadata = metadata
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Stripe subscription {SubscriptionId}", stripeSubscriptionId);
            throw;
        }
    }

    public async Task CancelSubscriptionAsync(string stripeSubscriptionId)
    {
        try
        {
            var service = new Stripe.SubscriptionService();
            await service.CancelAsync(stripeSubscriptionId);

            _logger.LogInformation("Canceled Stripe subscription {SubscriptionId}", stripeSubscriptionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling Stripe subscription {SubscriptionId}", stripeSubscriptionId);
            throw;
        }
    }

    public async Task<StripeCheckoutSessionInfo> GetCheckoutSessionAsync(string sessionId)
    {
        try
        {
            var service = new Stripe.Checkout.SessionService();
            var session = await service.GetAsync(sessionId);

            var metadata = new Dictionary<string, string>();
            if (session.Metadata != null)
            {
                foreach (var kvp in session.Metadata)
                {
                    metadata[kvp.Key] = kvp.Value;
                }
            }

            return new StripeCheckoutSessionInfo
            {
                Id = session.Id,
                PaymentStatus = session.PaymentStatus,
                Status = session.Status,
                CustomerId = session.CustomerId,
                SubscriptionId = session.SubscriptionId,
                Metadata = metadata
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Stripe checkout session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<DateTime> ExtendSubscriptionByMonthsAsync(string stripeSubscriptionId, int months)
    {
        if (months <= 0)
        {
            // No-op
            var info = await GetSubscriptionAsync(stripeSubscriptionId);
            return info.CurrentPeriodEnd;
        }

        var infoBefore = await GetSubscriptionAsync(stripeSubscriptionId);
        var newBillingAnchorUtc = DateTime.SpecifyKind(infoBefore.CurrentPeriodEnd.AddMonths(months), DateTimeKind.Utc);
        var newBillingAnchorUnix = new DateTimeOffset(newBillingAnchorUtc).ToUnixTimeSeconds();

        try
        {
            var service = new Stripe.SubscriptionService();
            var options = new SubscriptionUpdateOptions
            {
                ProrationBehavior = "none"
            };
            // Stripe.NET v50 models BillingCycleAnchor as now/unchanged. We need a timestamp, so use ExtraParams.
            options.AddExtraParam("billing_cycle_anchor", newBillingAnchorUnix);

            await service.UpdateAsync(stripeSubscriptionId, options);

            _logger.LogInformation(
                "Extended Stripe subscription {SubscriptionId} by {Months} month(s). New billing anchor: {BillingAnchor}",
                stripeSubscriptionId,
                months,
                newBillingAnchorUtc);

            return newBillingAnchorUtc;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extending Stripe subscription {SubscriptionId} by {Months} month(s)", stripeSubscriptionId, months);
            throw;
        }
    }
}

public interface IStripeService
{
    Task<string> CreateCustomerAsync(string userId, string email, string name);
    Task<string> CreateCheckoutSessionAsync(string userId, UserType userType, string tier, bool isAnnual, string? customerId = null, string? baseUrl = null);
    Task<StripeSubscriptionInfo> GetSubscriptionAsync(string stripeSubscriptionId);
    Task CancelSubscriptionAsync(string stripeSubscriptionId);
    Task<StripeCheckoutSessionInfo> GetCheckoutSessionAsync(string sessionId);
    Task<DateTime> ExtendSubscriptionByMonthsAsync(string stripeSubscriptionId, int months);
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
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class StripeCheckoutSessionInfo
{
    public required string Id { get; set; }
    public string? PaymentStatus { get; set; }
    public string? Status { get; set; }
    public string? CustomerId { get; set; }
    public string? SubscriptionId { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}