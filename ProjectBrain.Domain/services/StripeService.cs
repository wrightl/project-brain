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

    public async Task<string> CreateCheckoutSessionAsync(string userId, string userType, string tier, bool isAnnual, string? customerId = null)
    {
        try
        {
            var priceKey = $"{userType}_{tier}_{(isAnnual ? "Annual" : "Monthly")}";
            var priceId = _configuration[$"Stripe:PriceIds:{priceKey}"]
                ?? throw new InvalidOperationException($"Stripe price ID not found for {priceKey}");

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
                SuccessUrl = _configuration["Stripe:SuccessUrl"] ?? "https://localhost:3000/subscription/success?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = _configuration["Stripe:CancelUrl"] ?? "https://localhost:3000/subscription/cancel",
                Metadata = new Dictionary<string, string>
                {
                    { "userId", userId },
                    { "userType", userType },
                    { "tier", tier }
                },
                SubscriptionData = new Stripe.Checkout.SessionSubscriptionDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        { "userId", userId },
                        { "userType", userType },
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

            return new StripeSubscriptionInfo
            {
                Id = subscription.Id,
                Status = subscription.Status,
                CustomerId = subscription.CustomerId,
                TrialEnd = subscription.TrialEnd,
                CurrentPeriodStart = periodStart,
                CurrentPeriodEnd = periodEnd,
                PriceId = subscription.Items?.Data?.FirstOrDefault()?.Price?.Id
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
}

