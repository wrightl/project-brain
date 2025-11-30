using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using ProjectBrain.Domain;
using Stripe;

public class StripeWebhookServices(
    ILogger<StripeWebhookServices> logger,
    ISubscriptionService subscriptionService)
{
    public ILogger<StripeWebhookServices> Logger { get; } = logger;
    public ISubscriptionService SubscriptionService { get; } = subscriptionService;
}

public static class StripeWebhookEndpoints
{
    public static void MapStripeWebhookEndpoints(this WebApplication app)
    {
        // Stripe webhook endpoint - no authorization required (uses webhook secret)
        app.MapPost("/webhooks/stripe", HandleStripeWebhook)
            .WithName("StripeWebhook")
            .AllowAnonymous();
    }

    private static async Task<IResult> HandleStripeWebhook(
        [AsParameters] StripeWebhookServices services,
        HttpContext context,
        IConfiguration configuration)
    {
        var json = await new StreamReader(context.Request.Body).ReadToEndAsync();
        var webhookSecret = configuration["Stripe:WebhookSecret"];

        if (string.IsNullOrEmpty(webhookSecret))
        {
            services.Logger.LogError("Stripe webhook secret is not configured");
            return Results.BadRequest("Webhook secret not configured");
        }

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                context.Request.Headers["Stripe-Signature"],
                webhookSecret
            );

            services.Logger.LogInformation("Received Stripe webhook: {EventType}, ID: {EventId}", 
                stripeEvent.Type, stripeEvent.Id);

            // Use string comparison for event types (more reliable across Stripe.NET versions)
            var eventType = stripeEvent.Type;
            if (eventType == "customer.subscription.created" || eventType == "customer.subscription.updated")
            {
                await HandleSubscriptionUpdated(services, stripeEvent);
            }
            else if (eventType == "customer.subscription.deleted")
            {
                await HandleSubscriptionDeleted(services, stripeEvent);
            }
            else if (eventType == "invoice.payment_succeeded")
            {
                await HandleInvoicePaymentSucceeded(services, stripeEvent);
            }
            else if (eventType == "invoice.payment_failed")
            {
                await HandleInvoicePaymentFailed(services, stripeEvent);
            }
            else
            {
                services.Logger.LogInformation("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
            }

            return Results.Ok();
        }
        catch (StripeException ex)
        {
            services.Logger.LogError(ex, "Stripe webhook error: {Message}", ex.Message);
            return Results.BadRequest($"Webhook error: {ex.Message}");
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error processing Stripe webhook");
            return Results.Problem("Error processing webhook");
        }
    }

    private static async Task HandleSubscriptionUpdated(StripeWebhookServices services, Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (subscription == null)
        {
            services.Logger.LogWarning("Subscription object is null in webhook event");
            return;
        }

        await services.SubscriptionService.UpdateSubscriptionFromStripeAsync(subscription.Id);
        services.Logger.LogInformation("Updated subscription {SubscriptionId} from webhook", subscription.Id);
    }

    private static async Task HandleSubscriptionDeleted(StripeWebhookServices services, Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (subscription == null)
        {
            services.Logger.LogWarning("Subscription object is null in webhook event");
            return;
        }

        await services.SubscriptionService.UpdateSubscriptionFromStripeAsync(subscription.Id);
        services.Logger.LogInformation("Handled subscription deletion for {SubscriptionId}", subscription.Id);
    }

    private static async Task HandleInvoicePaymentSucceeded(StripeWebhookServices services, Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Stripe.Invoice;
        if (invoice == null)
        {
            return;
        }

        // Get subscription ID using reflection (property name may vary by Stripe.NET version)
        string? subscriptionId = null;
        var subscriptionProp = typeof(Stripe.Invoice).GetProperty("Subscription") 
            ?? typeof(Stripe.Invoice).GetProperty("SubscriptionId");
        
        if (subscriptionProp != null)
        {
            var value = subscriptionProp.GetValue(invoice);
            subscriptionId = value?.ToString();
        }

        if (subscriptionId == null)
        {
            services.Logger.LogWarning("Could not find subscription ID in invoice");
            return;
        }

        await services.SubscriptionService.UpdateSubscriptionFromStripeAsync(subscriptionId);
        services.Logger.LogInformation("Payment succeeded for subscription {SubscriptionId}", subscriptionId);
    }

    private static async Task HandleInvoicePaymentFailed(StripeWebhookServices services, Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Stripe.Invoice;
        if (invoice == null)
        {
            return;
        }

        // Get subscription ID using reflection (property name may vary by Stripe.NET version)
        string? subscriptionId = null;
        var subscriptionProp = typeof(Stripe.Invoice).GetProperty("Subscription") 
            ?? typeof(Stripe.Invoice).GetProperty("SubscriptionId");
        
        if (subscriptionProp != null)
        {
            var value = subscriptionProp.GetValue(invoice);
            subscriptionId = value?.ToString();
        }

        if (subscriptionId == null)
        {
            services.Logger.LogWarning("Could not find subscription ID in invoice");
            return;
        }

        await services.SubscriptionService.UpdateSubscriptionFromStripeAsync(subscriptionId);
        services.Logger.LogWarning("Payment failed for subscription {SubscriptionId}", subscriptionId);
    }
}

