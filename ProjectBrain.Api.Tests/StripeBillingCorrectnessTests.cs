using FluentAssertions;
using Stripe;

namespace ProjectBrain.Api.Tests;

public class StripeBillingCorrectnessTests
{
    [Fact]
    public void ResolveSubscriptionPeriod_UsesSubscriptionItemDates()
    {
        var created = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodStart = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);

        var subscription = new Subscription
        {
            Created = created,
            Items = new StripeList<SubscriptionItem>
            {
                Data =
                [
                    new SubscriptionItem
                    {
                        CurrentPeriodStart = periodStart,
                        CurrentPeriodEnd = periodEnd
                    }
                ]
            }
        };

        var (start, end) = ProjectBrain.Domain.StripeService.ResolveSubscriptionPeriod(subscription);

        start.Should().Be(periodStart);
        end.Should().Be(periodEnd);
        // Guard against the old Created / Created+1 month fallback.
        end.Should().NotBe(created.AddMonths(1));
    }

    [Fact]
    public void ResolveSubscriptionPeriod_WithMultipleItems_UsesMinStartAndMaxEnd()
    {
        var subscription = new Subscription
        {
            Created = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Items = new StripeList<SubscriptionItem>
            {
                Data =
                [
                    new SubscriptionItem
                    {
                        CurrentPeriodStart = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                        CurrentPeriodEnd = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)
                    },
                    new SubscriptionItem
                    {
                        CurrentPeriodStart = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc),
                        CurrentPeriodEnd = new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc)
                    }
                ]
            }
        };

        var (start, end) = ProjectBrain.Domain.StripeService.ResolveSubscriptionPeriod(subscription);

        start.Should().Be(new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc));
        end.Should().Be(new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ResolveInvoiceSubscriptionId_ReadsParentSubscriptionDetails()
    {
        var invoice = new Invoice
        {
            Id = "in_test",
            Parent = new InvoiceParent
            {
                Type = "subscription_details",
                SubscriptionDetails = new InvoiceParentSubscriptionDetails
                {
                    SubscriptionId = "sub_from_parent"
                }
            }
        };

        var subscriptionId = StripeWebhookEndpoints.ResolveInvoiceSubscriptionId(invoice);

        subscriptionId.Should().Be("sub_from_parent");
    }

    [Fact]
    public void ResolveInvoiceSubscriptionId_ReturnsNull_WhenParentMissing()
    {
        var invoice = new Invoice { Id = "in_no_parent" };

        // Stripe.net 52 removed Invoice.Subscription / SubscriptionId; without Parent this must be null.
        typeof(Invoice).GetProperty("Subscription").Should().BeNull();
        typeof(Invoice).GetProperty("SubscriptionId").Should().BeNull();

        StripeWebhookEndpoints.ResolveInvoiceSubscriptionId(invoice).Should().BeNull();
    }
}
