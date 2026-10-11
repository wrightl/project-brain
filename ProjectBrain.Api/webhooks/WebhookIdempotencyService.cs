using System.Security.Cryptography;
using System.Text;

namespace ProjectBrain.Api.Webhooks;

public enum WebhookClaimResult
{
    Claimed,
    AlreadyProcessed,
    InProgress
}

/// <summary>
/// Deduplicates webhook deliveries. An event is claimed before handling and only recorded as processed
/// once handling succeeds; a failed event must be abandoned so the provider's retry is not skipped.
/// </summary>
public interface IWebhookIdempotencyService
{
    WebhookClaimResult TryClaim(string provider, string? eventId);
    void MarkProcessed(string provider, string? eventId, TimeSpan ttl);
    void Abandon(string provider, string? eventId);
}

public static class WebhookSecurity
{
    public static bool IsValidBearerToken(string? providedToken, string? expectedToken)
    {
        if (string.IsNullOrEmpty(providedToken) || string.IsNullOrEmpty(expectedToken))
        {
            return false;
        }

        var providedBytes = Encoding.UTF8.GetBytes(providedToken);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedToken);

        return providedBytes.Length == expectedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }

    public static string ComputeSha256Hex(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash);
    }
}
